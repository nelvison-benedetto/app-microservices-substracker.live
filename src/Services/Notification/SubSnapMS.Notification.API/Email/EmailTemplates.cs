using System.Globalization;
using System.Net;
using Microsoft.Extensions.Options;
using SubSnapMS.Contracts.Subscriptions;
using SubSnapMS.Notification.API.Configuration;

namespace SubSnapMS.Notification.API.Email;

public sealed record RenewalReminderModel(
    string SubscriptionName,
    decimal Amount,
    string Currency,
    string BillingCycle,
    DateOnly RenewalDate);

//Template in italiano. Ogni valore inserito dall'utente (es. nome abbonamento) passa da Encode: un nome come "&lt;a href=...&gt;" non diventa un link nell'email.
public sealed class EmailTemplates(IOptions<EmailOptions> options)
{
    private static readonly CultureInfo Italian = CultureInfo.GetCultureInfo("it-IT");

    private string AppUrl => options.Value.AppBaseUrl.TrimEnd('/');

    public EmailMessage Welcome(string to)
    {
        const string subject = "Benvenuto su SubSnap";
        var dashboardUrl = $"{AppUrl}/dashboard";

        var html = Layout(subject, $"""
            <p style="margin:0 0 16px;">Ciao,</p>
            <p style="margin:0 0 16px;">il tuo account SubSnap è pronto. Aggiungi i tuoi abbonamenti:
            ti avviseremo prima di ogni rinnovo, così niente addebiti a sorpresa.</p>
            {Button("Vai alla dashboard", dashboardUrl)}
            """);

        var text = $"""
            Ciao,

            il tuo account SubSnap è pronto. Aggiungi i tuoi abbonamenti: ti avviseremo prima di ogni rinnovo, così niente addebiti a sorpresa.

            Vai alla dashboard: {dashboardUrl}
            """;

        return new EmailMessage(to, subject, html, text);
    }

    public EmailMessage RenewalReminder(string to, RenewalReminderModel model, DateOnly today)
    {
        var when = DescribeWhen(model.RenewalDate, today);
        var date = model.RenewalDate.ToString("dddd d MMMM yyyy", Italian);
        var amount = FormatAmount(model.Amount, model.Currency);
        var cycle = DescribeCycle(model.BillingCycle);
        var subject = $"{model.SubscriptionName} si rinnova {when}";
        var dashboardUrl = $"{AppUrl}/dashboard";
        var name = Encode(model.SubscriptionName);

        var html = Layout(subject, $"""
            <p style="margin:0 0 16px;">Ciao,</p>
            <p style="margin:0 0 16px;">il tuo abbonamento <strong>{name}</strong> si rinnova <strong>{Encode(when)}</strong>.</p>
            <table role="presentation" cellpadding="0" cellspacing="0" style="width:100%;margin:0 0 24px;border:1px solid #e4e4e7;border-radius:8px;">
              <tr><td style="padding:12px 16px;color:#71717a;">Data</td><td style="padding:12px 16px;text-align:right;">{Encode(date)}</td></tr>
              <tr><td style="padding:12px 16px;color:#71717a;border-top:1px solid #e4e4e7;">Importo</td><td style="padding:12px 16px;text-align:right;border-top:1px solid #e4e4e7;"><strong>{Encode(amount)}</strong></td></tr>
              <tr><td style="padding:12px 16px;color:#71717a;border-top:1px solid #e4e4e7;">Rinnovo</td><td style="padding:12px 16px;text-align:right;border-top:1px solid #e4e4e7;">{Encode(cycle)}</td></tr>
            </table>
            <p style="margin:0 0 16px;">Se non ti serve più, è il momento giusto per disdirlo.</p>
            {Button("Apri SubSnap", dashboardUrl)}
            """);

        var text = $"""
            Ciao,

            il tuo abbonamento {model.SubscriptionName} si rinnova {when}.

            Data: {date}
            Importo: {amount}
            Rinnovo: {cycle}

            Se non ti serve più, è il momento giusto per disdirlo.

            Apri SubSnap: {dashboardUrl}
            """;

        return new EmailMessage(to, subject, html, text);
    }

    // Formattazione

    private static string DescribeWhen(DateOnly renewalDate, DateOnly today) =>
        (renewalDate.DayNumber - today.DayNumber) switch
        {
            <= 0 => "oggi",
            1 => "domani",
            var days => $"tra {days} giorni"
        };

    private static string FormatAmount(decimal amount, string currency)
    {
        var value = amount.ToString("N2", Italian);
        return currency == "EUR" ? $"{value} €" : $"{value} {currency}";
    }

    private static string DescribeCycle(string billingCycle) => billingCycle switch
    {
        BillingCycles.Weekly => "settimanale",
        BillingCycles.Monthly => "mensile",
        BillingCycles.Quarterly => "trimestrale",
        BillingCycles.Biannual => "semestrale",
        BillingCycles.Yearly => "annuale",
        _ => billingCycle
    };

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static string Button(string label, string url) => $"""
        <p style="margin:24px 0 0;">
          <a href="{Encode(url)}" style="display:inline-block;background:#18181b;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:8px;font-weight:bold;">{Encode(label)}</a>
        </p>
        """;

    //Layout a tabelle con stili inline: l'unico HTML che i client email (Outlook, Gmail) rendono bene
    private static string Layout(string title, string content) => $"""
        <!DOCTYPE html>
        <html lang="it">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>{Encode(title)}</title>
        </head>
        <body style="margin:0;padding:0;background:#f4f4f5;font-family:Arial,Helvetica,sans-serif;color:#18181b;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f4f4f5;padding:24px 12px;">
            <tr>
              <td align="center">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:#ffffff;border-radius:12px;">
                  <tr><td style="padding:32px 32px 8px;font-size:20px;font-weight:bold;">SubSnap</td></tr>
                  <tr><td style="padding:8px 32px 32px;font-size:15px;line-height:1.6;">{content}</td></tr>
                </table>
                <p style="font-size:12px;color:#71717a;margin:16px 0 0;">Ricevi questa email perché hai un account su SubSnap.</p>
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}