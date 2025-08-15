using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;

const string SpreadsheetName = "Drive File List";
string[] scopes = { DriveService.Scope.Drive, SheetsService.Scope.Spreadsheets };
var applicationName = "A815";
UserCredential credential;
using (var stream =
       new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
{
    string tokenPath = "token.json";
    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
        GoogleClientSecrets.FromStream(stream).Secrets,
        scopes,
        "user",
        CancellationToken.None,
        new FileDataStore(tokenPath, true));
}

var driveService = new DriveService(new BaseClientService.Initializer()
{
    HttpClientInitializer = credential,
    ApplicationName = applicationName,
});

var sheetsService = new SheetsService(new BaseClientService.Initializer()
{
    HttpClientInitializer = credential,
    ApplicationName = applicationName,
});

while (true)
{
    Console.WriteLine($"{DateTime.Now} Sync files");
    string spreadsheetId = await GetOrCreateSpreadsheetIdAsync(driveService, sheetsService);
    var files = await GetDriveFilesAsync(driveService);
    await UpdateSpreadsheetAsync(sheetsService, spreadsheetId, files);
    Console.WriteLine($"{DateTime.Now} Spreadsheet updated successfully!");
    await Task.Delay(TimeSpan.FromMinutes(15));
}

static async Task<string> GetOrCreateSpreadsheetIdAsync(DriveService driveService, SheetsService sheetsService)
{
    var listRequest = driveService.Files.List();
    listRequest.Q = $"name='{SpreadsheetName}' and mimeType='application/vnd.google-apps.spreadsheet' and trashed=false";
    listRequest.Fields = "files(id, name)";
    var existing = await listRequest.ExecuteAsync();

    if (existing.Files != null && existing.Files.Count > 0)
    {
        Console.WriteLine($"Found existing spreadsheet: {existing.Files[0].Id}");
        return existing.Files[0].Id;
    }

    var spreadsheet = new Google.Apis.Sheets.v4.Data.Spreadsheet()
    {
        Properties = new SpreadsheetProperties() { Title = SpreadsheetName }
    };
    var createRequest = sheetsService.Spreadsheets.Create(spreadsheet);
    var created = await createRequest.ExecuteAsync();

    Console.WriteLine($"Created new spreadsheet: {created.SpreadsheetId}");
    return created.SpreadsheetId;
}

static async Task<List<(string Name, string Id)>> GetDriveFilesAsync(DriveService driveService)
{
    var request = driveService.Files.List();
    request.PageSize = 100;
    request.Fields = "files(id, name)";
    var result = await request.ExecuteAsync();

    var list = result.Files?.Select(f => (f.Name, f.Id)).ToList() ?? new List<(string, string)>();
    Console.WriteLine($"Retrieved {list.Count} files.");
    return list;
}

static async Task UpdateSpreadsheetAsync(SheetsService sheetsService, string spreadsheetId, List<(string Name, string Id)> files)
{
    var values = new List<IList<object>>();
    values.Add(new List<object> { "File Name", "File ID" });
    foreach (var file in files)
    {
        values.Add(new List<object> { file.Name, file.Id });
    }

    var valueRange = new ValueRange { Values = values };

    var clearRequest = sheetsService.Spreadsheets.Values.Clear(new ClearValuesRequest(), spreadsheetId, "A:Z");
    await clearRequest.ExecuteAsync();

    var updateRequest = sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, "A1");
    updateRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
    await updateRequest.ExecuteAsync();
}
