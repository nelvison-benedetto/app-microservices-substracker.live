### Platform Microservices (evolution of monolith SubsTracker)

- Author: **Nelvison Benedetto**
- Project: SubSnap Backend Microservices
- Platform: ASP.NET Core (.NET 10)
- Maturity Level: Production-grade backend foundation

Evoluzione della monolith platform SubsTracker https://github.com/nelvison-benedetto/app-fullstack-substracker.live <br>

struttura a docker containers

```
                                              INTERNET
                                                 |
                                               :443
                                                 |
                                                 v
                                            +----------+
                                            | TRAEFIK  |
                                            +----+-----+
                                                 |
                                                 v
                                            +-----------+
                                            |API GATEWAY|
                                            +----+------+
                                                 |
       +--------------------+--------------------+--------------------+--------
       |                    |                    |                    |
       v                    v                    v                    v
+-------------+    +----------------+    +-------------+    +-------------+
| IDENTITY    |    | SUBSCRIPTIONS  |    | MEDIA       |    | ANALYTICS   |
| API         |    | API            |    | API         |    | API         |
+------+------+    +-------+--------+    +------+------+    +------+------+
       |                   |                    |                    |
       v                   v                    v                    v
+-------------+    +----------------+    +-------------+    +-------------+
| PostgreSQL  |    | PostgreSQL     |    | PostgreSQL  |    | MongoDB     |
| (identity)  |    | (subscriptions)|    | (media)     |    | (analytics) |
+-------------+    +----------------+    +-------------+    +-------------+


-------+------+--------------------+--------------------+
              |                    |                    |
              v                    v                    v
       +-------------+      +-------------+     +----------------+
       | SEARCH      |      | GRAPH       |     | NOTIFICATION    |
       | API         |      | API         |     | API             |
       +------+------+      +------+------+     +-------+--------+
              |                    |                    |
              v                    v                    v
       +-------------+      +-------------+     +----------------+
       |Elasticsearch|      | Neo4j       |     | MongoDB        |
       |             |      |             |     | (notification) |
       +-------------+      +-------------+     +----------------+
```

## Traefik -> YARP -> services

- Traefik: (better than Nginx x docker containers) è il reverse proxy, legge automaticamente le label docker dei services e si configura da solo. Si occupa di scrivere X-Forwarded-For e X-Forwarded-Proto, redirect http->https, TLS con certificati Let's Encrypt auto, routing (nel mio caso verso porta docker del mio gateway), e svuota traceparent/tracestate (trace info) in ingresso (cosi hacker non puo iniettare trace fake). 
//X-Forwarded-For è indirizzo IP del client, X-Forwarded-Proto è il protocollo utilizzato dal client per la req (http or https)

- YARP: (better than Ocelot) è l'API Gateway cioè l'unico punto di ingresso, conosce tutti i services. Yarp ragiorna per Routes(quale req) e Clusters(dove mandarla, un cluster può avere multiple destinazioni e yarp bilancia il carico) .Si occupa di validare il JWT (cmnq ogni services lo ri-valida), applica i limiti per utente (e.g. max 10req/minuto x auth, e le **polly policies**) e gli envelop json. 
Attualmente non utilizzo **BackendForFrontend** (BFF), cioè un portale dedicato e.g. Web.bbf e Mobile.bbf che sono più di un "gateway di instradamento", xk aggregano i datas adttano i datas e gestiscono l'auth in modo diverso per il client.

- Auth: (better than HS256 used in the old monolith, cioè key segreta condivisa che serviva sia a firmare che verificare) uso **RS256** quindi solo service Identity ha la key privata e firma i tokens, gli altri (gateway compreso) hanno solo key pubbliche e possono solo verificare check.
Le key pubbliche sono distribuite in modo standart (come fanno Auht0, Azure AD e keycloak): 
```
GET http://identity:8080/.well-known/openid-configuration
  → { issuer, jwks_uri: ".../.well-known/jwks.json", alg: RS256 }

GET http://identity:8080/.well-known/jwks.json
  → { keys: [ { kty:"RSA", kid:"...", n:"...", e:"..." }, ... ] }
```
Quindi ogni service scarica il **JWKS** all'avvio (cache 5min) e verifica i tokens senza chiamare service Identity ad ogni req.
Utilizzo di ***Access Token (JWT)* che vale 10min e **Refresh Token** che vale 30days.
Utilizzo di techniques rotazione delle key di firma, rotazione dei refresh token con famiglie e reuse detection.

- Observability: (uso OpenTelemetry better than Serilog che offriva solo logs) offre logs, tracing e metriche.  
il contesto della trace (cioè 'traceparent', standart W3C Trace Context) viaggia negli header http(automatico) nelle chiamate gRPC(automatico) dentro l'Outbox (salvato nella row al momento della scrittura) e negli header dei mexs rabbitmq (span 'publish' di tipo Producer, span 'process' di tipo Consumer), in questo modo e.g. Grafana puo vedere in un'unica singola trace che parte da e.g. POST /api/v1/subscriptions fino all'indicizzazione in Elasticsearch e all'email inviata (anche across processi diversi in tempo lungo).
Nell'old monolith avevo 'Correlation Id' ora ho TraceId, restituito nell'heade X-Correlation-Id.
Utilizzo anche sempre lib Sentry per monitoring dei bugs sul server (50*).


## Comunicazione Sync e Comunicazione Async

- REST/HTTP (json): utilizzata dal frontend per fare una req, la risposta gli verrà consegnata nello stesso modo.
- gRPC : (rispetto a rest/http usa file .proto invece di json quindi meno bytes e http/2 piu moderno e supporta multiple chiamate su stessa connessione tcp, ma oggi sul web per standart si utilizza sempre rest/http) usato per comunicazione **Sync** tra services l'un l'altro, quando la un service richiede qualcosa ADESSO da un altro service.
- RabbitMQ : per comunicazione **Asyn** tra microservices, utilizza gli eventi (e.g. service Subscriprions pubblica un event di nuova Subscription creata, e services Analytics,Search e Graph lo leggono aggiornano i loro dati).
Here le queue sono **quorum queue**(replicate con Raft, durevoli) e ognuna ha la sua dead letter <service>.dlx (fanout) -> <service>.queue.dlq .
I messaggi sono persistent, e con publisher confirms attivi (cioè il publisher termina solo quando ha conferma che il broker ha preso in carico il mex).

- Transactional Outbox & Inbox consumer indempodence & retry/backoff/Dead letter Queue(DLQ): 
Transactional Outbox risolve il problema **dual write** cioè e.g. save on postgres & publish event on rabbitmq (è rischioso eseguirli in 2 transazioni xk e.g.se cade la connessione tra queste 2 transazioni allora ti ritrovi con datas salvati su postgres ma nessun event pubblicato! o l'inverso) quindi DEVI ESEGUIRE IN 1 TRANSAZIONE. 
quindi lo risolvo con:
Handler -> INSERT subscriptions (la mia new subscription on tab) & INSERT outbox_messages (è l'event x rabbitmq lo salvo su questa tab) -> COMMIT(or both or none!! COSI QUESTA E' TRANSAZIONE ATOMICA!!) on postgresql. E nel frattempo gira sempre in background OutboxProcessor che ogni 2sec legge su postgresql da tab 🔥**outbox_messages** le ultime 50rows (quindi gli ultimi 50 rabbitmq events) e li pubblica (con PUBLISH CONFIRM, cioè si deve assicurare che il worker lo ha preso in carico) dopodiché viene aggiornato field processed_on_utc=now per scrivere quando l'event è stato pubblicato.
Dal lato del consumer (e.g.Analytics che riceve event di una nuova subscription)  
ottengo 🔥**indempodenza**(cioè verifica se è gia stato processato!! here uso AND condition) usando in 1 SOLA TRANSAZIONE e tab 🔥**inbox**: 
messaggio arrivato 
 -> BEGIN
 -> SELECT EXISTS(inbox WHERE message_id= X AND handler=H) //se yes allora skip!!
 -> esegui l'handler (le sue scritture, eventuali nuovi eveti nell'Outbox, ect)
 -> INSERT inbox(message_id, handler)
 -> COMMIT         
-> ACK a rabbitmq 

- Casi di retry/backoff/Dead Letter Queue(DLQ)/Tombstone: 
shutdown durante l'elaborazione -> nessun ACK -> rabbitmq lo riconsegna.
payload illeggibile (json invalido) -> subito in DLQ (here li trovi nella dashboard di rabbitmq, e li puoi rimmettere in coda manualmente magari aggiustati.
handler fallisce
 -> retry in-process con backoff esponenziale (tempo di attesa rispetto all prev tentativo, esponenziale cioè 1sec-2sec-4sec-ect con scope ID nuovo ogni volta)
 -> ancora KO -> NACK requeue=false -> DLX -> <service>.queue.dlq (cioe in DLQ).
Quando e.g. un utente o subscription viene cancellata, prima di tutto lo si marca come 🔥**tombstone** con versione massima, così un evento in ritardo (e.g.SubscriptionUpdate) non puo usarlo, ed i tombstones gli assegni un TTL che scaduto quello allora vengono definitivamente eliminati (e.g.parte pattern Saga  DeleteUser che a cascata fa partire events per cancellare i datas di quell'utente anche negli altri services).


## DBs & Redis

- Database per Service: ogni service ha il suo db, **se un service ha bisogno di datas di un altro service allora grazie agli events si crea una propria copia**.
//per postgresql e mongodb, utilizzo 2 istanze condivise (per ecomicità) ma all'interno sono isolati per ruolo e db.
Dtaabse    |  Services
PostgreSQL - Identity, Subscriptions, Media
Redis 	   - Subscriptions
MongoDB    - Analytics, Notification
ElasticSearch - Search
Neo4j      - Graph 

- Redis (non utilizza mai un volume docker, solo ram): lo utilizzo solo per il service dove serve di più, quindi Subscriptions (x salvare in cache risposta di 'dammi la lista dei miei abbonamenti'), con key subscriptions:user:{userId}:list.
Utilizzo di **HybridCache** usando pattern **Cache-Aside**, cioè  req->L1->L2->db postgresql dove L1 è la cache ram del container subscriptions-api (velocissima, ogni clone di questo service ha la sua ram, key scade dopo 1min) e L2 è il container redis condiviso (tra tutti i clones del service, attualemnte solo x service Subscriptions) che contiene tutte le keys (key scade dopo 10min), quindi si legge sempre prima in ordine L1->L2->db postgresql(se si legge da qua allora subito dopo  HybridCache scrive su L2 e su L1). 
//attualmente su L2 ho solo le liste solo per gli utenti che le hanno chieste negli ultimi 10min.


## Techniques & Patterns

- Saga : sequenza di transazioni coordinate di mexs across services per completare una singola task, nella piattaforma la utilizzo solo x task cancellazione di un user (coinvolge tutti i services e cancella in ognuno i datas relativi a quell'utente) che va raggiunto per intero (non è Saga e.g. registrazione account che è solo "fire and forget"). 

- Microservice Chassis: nel code le funzionalità trasversali sono ciascuna in lib pubbliche, le mie BuildingBlocks.* .

- Event Versioning: ogni evento ha la version e.g."subscription.created.v1", e alla v1 puo aggiungere un campo facoltativo (e.g. diventa { "eventId": "…", "subscriptionId": "…", "userId": "…", "name": "Netflix", "amount": 12.99, "currency": "EUR", "category": "Streaming" }) ma se devi fare breaking changes (e.g.eliminare/rinominare un field) allora crei la v2.
Il rabbitmq producer pubblica entrambe le versioni (quindi vengono scritte 2 rows nell' Outbox tab), i consumer continuano a ricevere v1 come sempre, ma tu li migri uno alla volta  AddConsumer<SubscriptionCreated,...> -> AddConsumer<SubscriptionCreatedV2>  e RabbitMqConsumerService binda correttamente su ...v2 , quindi poi nessuno userà né riceverà v1 quindi lo potri togliere.

- here non utilizzo lib Polly (come invece la utilizzo in altri prjs a ms).


## Within Each Service

-   Services  |  N prjs
Identity, Subscriptions : usano 4 prjs (.Infrastructure, .Core, .Application e .API).
Media : usano 2 prjs (.Core e .API).
Analytics, Search, Graph, Notification: usano 1 prj (.API).

- x functionality Vertical Slice architecture: nell'old monolith avevo controller -> orchestrator -> handler divisi per tipo tecnico (e.g.), ora invece li divido per functionality cioè e.g.
.Application/Features/Subscriptions/Create 
  CreateSubscriptionCommand.cs
  CreateSubscriptionCommandValidator.cs
  CreateSubscriptionhandler.cs
Endpoint/CreateSubscriptionEndpoint.cs  //implementa my IEndpoint

- All techniques & patterns of monolith (clean architecture, vertical slice architecture this, CQRS, mediator+pipeline behaviors, DDD, repository+**unit of work**, **optimistic cuncurrency**() )
//optimistic cuncurrency: un'altra req ha modificato lo stesso aggregate tra la lettura e il salvataggio (e.g. due refresh simultanei dello stesso token). quindi se sai che i conflitti sono rari, allora usa Optimistic technique cioè non blocchi niente mentre ci lavori ma al salvataggio check che nessuno l'abbia cambiata (semplicemente sul db fai un UPDATE con un WHERE filtrando per id & ex-value che avevi letto tu). oppure se sai che I CONFLITTI SONO FREQUENTI allora blocca la riga db mentre ci lavori (usando (SELECT … FOR UPDATE), ovviamente questo può comportare rallentamenti e deadlocks) (see file ConcurrencyConflictException.cs )


## Docker

- 1 solo Dockerfile per tutti i services.
- 3 reti docker: 
[Traefik, Api gateway], //🔥SOLO traefik si espone a internet
[Api gateway, all services, all dbs],  //ovviamente non crei 2 containers Api gateway
[Media, Notification](questa rete deve poter uscire verso internet per collegarsi a cloudflare R2 e x notification emails)
- 1 docker-compose.yml e 1 docker-compose.override.yml (per fare override dell'original, x easy durante development phase).
- ovviamente sempre .env file scritto direttamente su server senza passare da GitHub (o Forgejo).
- tutti i miei containers sono 🔥**stateless** (i datas stanno nel db, i containers si non contegono datas si possono clonare easy) (invece i **stateful** contengono datas che devono sopravvivere, solitamente in 1 volume, ma il loro scaling è complicato perche dovresti avere un orchestrator che gestisce i datas dei clones. di solito in questo caso si paga il servizio cloud a pagamento).
- puoi scalare ogni service in modo indipendente con Kubernetes (anche l'Api gateway).

