using Microsoft.AspNetCore.Routing;

namespace SubSnapMS.BuildingBlocks.Web.Endpoints;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder routes);  //"registrati sulle route"
}

/*
in SubSnap avevi 
SubscriptionsController   (tutti gli endpoint degli abbonamenti in una classe)
   └─ SubscriptionsOrchestrator   (coordinava la logica)
        └─ servizi, repository…

ora IEndpoint sostituisce Controller e Orchestrator del ex monolith, l'endpoint traduce HTTP → costruisce command mediatR → ISender (cosi che classi che lo implementano vengono trovate e mappate in automatico). quindi ora ogni operazione è una classe a sè in 
Features/
├── RequestUpload/
│   ├── RequestUploadCommand.cs
│   ├── RequestUploadCommandValidator.cs
│   ├── RequestUploadHandler.cs
│   └── RequestUploadEndpoint.cs   ← implementa IEndpoint (OGNI ENDPOINT implementa questa interface!)
├── ConfirmUpload/
│   └── …

quindi nel monlith mettevi tutti i controller da una parte, e tutti i servizi dall’altra. ora invece DIVIDI IN FOLDERS PER FUNCTIONALITY (e.g.tutto cio che richiede un upload lo metti in RequestUpload), è VERTICAL SLICE ARCHITECTURE!!

*/

