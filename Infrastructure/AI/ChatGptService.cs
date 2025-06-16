using ManufacturingScheduler.Core.Interfaces;
using ManufacturingScheduler.Core.Models;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ManufacturingScheduler.Infrastructure.AI
{
    using System.Text.RegularExpressions;
    public class ChatGptService : IChatGptService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly ILogger<ChatGptService> _logger;

        public ChatGptService(HttpClient httpClient, string apiKey, string endpoint, ILogger<ChatGptService> logger)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _endpoint = endpoint;
            _logger = logger;

            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        public async Task<SchedulingInterpretation> InterpretSchedulingRequestAsync(string request, ProductionSchedule currentSchedule)
        {
            try
            {
                var prompt = BuildSchedulingPrompt(request, currentSchedule);
                var aiResponse = await CallOpenAIAsync(prompt); 

                // AI-Antwort in strukturierte Daten umwandeln
                return ParseAIResponseToInterpretation(aiResponse, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error interpreting scheduling request");
                return new SchedulingInterpretation
                {
                    SuggestedChanges = new List<ScheduleChange>(),
                    ExplanationText = "Fehler beim Verarbeiten der Anfrage",
                    IsValid = false
                };
            }
        }

        public async Task<string> AnalyzeScheduleAsync(ProductionSchedule schedule)
        {
            try
            {
                var prompt = BuildAnalysisPrompt(schedule);
                var analysis = await CallOpenAIAsync(prompt);
                // return analysis;
                return AddColorToAnalysis(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing schedule");
                return $"Plan enthält {schedule.ScheduleItems.Count} Positionen. Analyse vorübergehend nicht verfügbar.";
            }
        }

        private string BuildSchedulingPrompt(string request, ProductionSchedule schedule)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Du bist ein KI-Assistent für die Fertigungsplanung. Antworte immer auf Deutsch. Analysiere diese Anfrage und gib eine Antwort, indem du diese Punkte ausfüllst: Aktueller Status, Risiken, Vorschläge. Füge die Aufzählungspunkte in deine Antwort ein.");
            sb.AppendLine($"User Request: {request}");
            sb.AppendLine($"Current Schedule Information:");
            sb.AppendLine($"- Schedule ID: {schedule.Id}");
            sb.AppendLine($"- Created: {schedule.CreatedDate} by {schedule.CreatedBy}");
            sb.AppendLine($"- Total Items: {schedule.ScheduleItems.Count}");
            sb.AppendLine($"- Completion: {schedule.CompletionPercentage:F1}% complete");
            sb.AppendLine($"- Current Status: {schedule.CompletedItems} completed, {schedule.PendingItems} pending");
            sb.AppendLine();

            // Elemente nach Status gruppieren für bessere Übersicht
            var completedItems = schedule.ScheduleItems.Where(i => i.Status == ScheduleItemStatus.Completed).ToList();
            var pendingItems = schedule.ScheduleItems.Where(i => i.Status == ScheduleItemStatus.Planned).ToList();
            var inProgressItems = schedule.ScheduleItems.Where(i => i.Status == ScheduleItemStatus.InProgress).ToList();

            if (completedItems.Any())
            {
                sb.AppendLine("ABGESCHLOSSENE AUFTRÄGE (bereits fertig):");
                foreach (var item in completedItems)
                {
                    var actualEnd = item.ActualEndTime?.ToString("MM/dd HH:mm") ?? "unknown";
                    var timeDiff = item.ActualEndTime.HasValue ? (item.EndTime - item.ActualEndTime.Value).TotalHours : 0;
                    var status = timeDiff > 0 ? $"(fertig {timeDiff:F1}h früher)" : $"(fertig {Math.Abs(timeDiff):F1}h später)";

                    sb.AppendLine($"✅ Auftrag {item.OrderId} auf Maschine {item.MachineId}: ABGESCHLOSSEN um {actualEnd} {status}");
                    sb.AppendLine($"   Ursprünglich geplant: {item.StartTime:MM/dd HH:mm} - {item.EndTime:MM/dd HH:mm}");
                    if (!string.IsNullOrEmpty(item.Notes))
                        sb.AppendLine($"   Notizen: {item.Notes}");
                }
                sb.AppendLine();
            }

            if (inProgressItems.Any())
            {
                sb.AppendLine("LAUFENDE AUFTRÄGE (werden gerade bearbeitet):");
                foreach (var item in inProgressItems)
                {
                    sb.AppendLine($"🔄 Auftrag {item.OrderId} auf Maschine {item.MachineId}: LAUFEND");
                    sb.AppendLine($"   Geplant: {item.StartTime:MM/dd HH:mm} - {item.EndTime:MM/dd HH:mm} ({item.PlannedQuantity} Einheiten)");
                    if (item.ActualStartTime.HasValue)
                        sb.AppendLine($"   Tatsächlich gestartet: {item.ActualStartTime:MM/dd HH:mm}");
                }
                sb.AppendLine();
            }

            if (pendingItems.Any())
            {
                sb.AppendLine("AUSSTEHENDE AUFTRÄGE (noch nicht gestartet):");
                foreach (var item in pendingItems)
                {
                    sb.AppendLine($"⏳ Auftrag {item.OrderId} auf Maschine {item.MachineId}: AUSSTEHEND");
                    sb.AppendLine($"   Geplant: {item.StartTime:MM/dd HH:mm} - {item.EndTime:MM/dd HH:mm} ({item.PlannedQuantity} Einheiten)");
                }
                sb.AppendLine();
            }

            sb.AppendLine("WICHTIGE REGELN:");
            sb.AppendLine("- ✅ ABGESCHLOSSENE Aufträge können nicht verschoben oder umgeplant werden - sie sind bereits fertig!");
            sb.AppendLine("- 🔄 LAUFENDE Aufträge sollten normalerweise nicht verschoben werden, außer in kritischen Fällen");
            sb.AppendLine("- ⏳ AUSSTEHENDE Aufträge können frei umgeplant werden");
            sb.AppendLine("- Schlage nur Änderungen für AUSSTEHENDE Aufträge vor");
            sb.AppendLine("- Wenn ein abgeschlossener Auftrag erwähnt wird, bestätige, dass er bereits fertig ist");
            sb.AppendLine();

            sb.AppendLine("Antworte im Fließtext mit folgender Struktur:");
            sb.AppendLine("1. AKTUELLER STATUS: Beschreibe den aktuellen Zustand des Zeitplans");
            sb.AppendLine("2. ANALYSE: Identifiziere Probleme und Verbesserungsmöglichkeiten");
            sb.AppendLine("3. EMPFEHLUNGEN: Gib konkrete Vorschläge für Optimierungen");
            sb.AppendLine("");
            sb.AppendLine("Verwende klare Absätze und halte die Antwort prägnant und umsetzbar.");
            sb.AppendLine();
            sb.AppendLine("Verfügbare Aktionen:");
            sb.AppendLine("- 'start' oder 'begin': Status auf InProgress ändern");
            sb.AppendLine("- 'complete' oder 'finish': Status auf Completed ändern");
            sb.AppendLine("- 'cancel': Status auf Cancelled ändern");

            return sb.ToString();
        }

        private string BuildAnalysisPrompt(ProductionSchedule schedule)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Analysiere diesen Produktionsplan und gib Einblicke. Antworte immer auf Deutsch:");
            sb.AppendLine($"Created: {schedule.CreatedDate} by {schedule.CreatedBy}");
            sb.AppendLine($"Overall Progress: {schedule.CompletionPercentage:F1}% complete");
            sb.AppendLine($"Items: {schedule.CompletedItems} completed, {schedule.PendingItems} pending, {schedule.InProgressItems} in progress");
            sb.AppendLine();

            var completedItems = schedule.ScheduleItems.Where(i => i.Status == ScheduleItemStatus.Completed).ToList();
            var pendingItems = schedule.ScheduleItems.Where(i => i.Status == ScheduleItemStatus.Planned).ToList();

            if (completedItems.Any())
            {
                sb.AppendLine("ABGESCHLOSSENE ARBEIT:");
                foreach (var item in completedItems)
                {
                    var actualEnd = item.ActualEndTime?.ToString("MM/dd HH:mm") ?? "unknown";
                    var timeDiff = item.ActualEndTime.HasValue ? (item.EndTime - item.ActualEndTime.Value).TotalHours : 0;
                    var efficiency = timeDiff > 0 ? "vor Zeitplan" : "hinter Zeitplan";

                    sb.AppendLine($"- Auftrag {item.OrderId}: Abgeschlossen um {actualEnd} ({Math.Abs(timeDiff):F1}h {efficiency})");
                }
                sb.AppendLine();
            }

            if (pendingItems.Any())
            {
                sb.AppendLine("VERBLEIBENDE ARBEIT:");
                foreach (var item in pendingItems)
                {
                    sb.AppendLine($"- Auftrag {item.OrderId} auf Maschine {item.MachineId}: {item.StartTime:MM/dd HH:mm} - {item.EndTime:MM/dd HH:mm}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("Gib eine prägnante Analyse (unter 1000 Wörter) mit folgenden Punkten:");
            sb.AppendLine("- Gesamtleistung: Fertigstellungsgrad %, Zeitplan und Effizienz");
            sb.AppendLine("- Zeiteinsparungen: vorzeitige Fertigstellungen und Auswirkungen");
            sb.AppendLine("- Potenzielle Optimierungen für verbleibende Arbeit");
            sb.AppendLine("- Hauptengpässe und unterausgelastete Ressourcen");
            sb.AppendLine("");
            sb.AppendLine("Verwende einfache Formatierung. Fokussiere auf umsetzbare Erkenntnisse, nicht auf detaillierte Erklärungen.");

            return sb.ToString();
        }

        private async Task<string> CallOpenAIAsync(string prompt)
        {
            var requestBody = new
            {
                // model = "gpt-3.5-turbo",
                model = "gpt-4",
                messages = new[]
                {
                    new { role = "system", content = "Antworte immer auf Deutsch. Du bist ein KI-Assistent für die Fertigungsplanung." },
                    new { role = "user", content = prompt }
                },
                max_tokens = 1000,
                temperature = 0.7
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var responseObj = JsonSerializer.Deserialize<JsonElement>(responseJson);

            return responseObj.GetProperty("choices")[0]
                              .GetProperty("message")
                              .GetProperty("content")
                              .GetString() ?? "No response";
        }
        private SchedulingInterpretation ParseAIResponseToInterpretation(string aiResponse, string originalRequest)
        {
            try
            {
                return new SchedulingInterpretation
                {
                    SuggestedChanges = new List<ScheduleChange>(),
                    ExplanationText = aiResponse,
                    IsValid = !string.IsNullorWhiteSpace(aiResponse)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing AI response");

                return new SchedulingInterpretation
                {
                    SuggestedChanges = new List<ScheduleChange>(),
                    ExplanationText = $"Error parsing AI response: {aiResponse}",
                    IsValid = false
                };
            }
        }

        private bool TryGetIntFromJsonElement(JsonElement element, string propertyName, out int value)
        {
            value = 0;

            if (!element.TryGetProperty(propertyName, out var property))
                return false;

            // Handle numeric values
            if (property.ValueKind == JsonValueKind.Number)
            {
                return property.TryGetInt32(out value);
            }

            // Handle string values that might be numbers
            if (property.ValueKind == JsonValueKind.String)
            {
                var stringValue = property.GetString();
                if (string.IsNullOrEmpty(stringValue) || stringValue == "N/A")
                    return false;

                return int.TryParse(stringValue, out value);
            }

            return false;
        }

        private List<ScheduleChange> ExtractChangesFromText(string aiResponse, string originalRequest)
        {
            var changes = new List<ScheduleChange>();

            // Simple text parsing for common patterns
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Look for patterns like "Order 1" or "move order 2"
                if (line.ToLower().Contains("order") && line.ToLower().Contains("machine"))
                {
                    // Extract order ID and machine ID using regex or simple parsing
                    var orderMatch = System.Text.RegularExpressions.Regex.Match(line, @"order\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    var machineMatch = System.Text.RegularExpressions.Regex.Match(line, @"machine\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                    if (orderMatch.Success && machineMatch.Success)
                    {
                        changes.Add(new ScheduleChange
                        {
                            OrderId = int.Parse(orderMatch.Groups[1].Value),
                            NewMachineId = int.Parse(machineMatch.Groups[1].Value),
                            Reason = line.Trim()
                        });
                    }
                }
            }

            return changes;

        }


        private string AddColorToAnalysis(string analysis)
        {
            // Farbcodes mit \u001b statt \033 [Das war der entscheidende Unterschied für die Funktionalität]
            const string RESET = "\u001b[0m";
            const string BOLD = "\u001b[1m";
            const string RED = "\u001b[1;31m";
            const string GREEN = "\u001b[1;32m";
            const string YELLOW = "\u001b[1;33m";
            const string BLUE = "\u001b[1;34m";
            const string CYAN = "\u001b[1;36m";

            // Use regex for dynamic patterns
            analysis = Regex.Replace(analysis, @"\*\*(.*?)\*\*", $"{CYAN}{BOLD}**$1**{RESET}");

            // Color percentages based on value
            analysis = Regex.Replace(analysis, @"(\d+(?:\.\d+)?)%", match =>
            {
                var percentage = double.Parse(match.Groups[1].Value);
                var color = percentage == 0 ? RED : percentage < 50 ? YELLOW : GREEN;
                return $"{color}{match.Value}{RESET}";
            });

            // Color fractions (like 0/5, 3/10)
            analysis = Regex.Replace(analysis, @"(\d+)/(\d+)", match =>
            {
                var completed = int.Parse(match.Groups[1].Value);
                var total = int.Parse(match.Groups[2].Value);
                var color = completed == 0 ? RED : completed == total ? GREEN : YELLOW;
                return $"{color}{match.Value}{RESET}";
            });

            // Color time durations - preserve original text exactly
            analysis = Regex.Replace(analysis, @"(\d+(?:\.\d+)?)\s+(days?|hours?|minutes?)", match =>
            {
                return $"{YELLOW}{match.Value}{RESET}";
            });

            // Color dates/times (6/6, 6/8, etc.)
            analysis = Regex.Replace(analysis, @"(\d{1,2}/\d{1,2}(?:\s+\d{2}:\d{2})?)", $"{CYAN}$1{RESET}");

            // Color machine numbers
            analysis = Regex.Replace(analysis, @"Machine\s+(\d+)", $"Machine {BLUE}$1{RESET}");

            // Color order numbers  
            analysis = Regex.Replace(analysis, @"Order\s+(\d+)", $"Order {BLUE}$1{RESET}");

            // Color warning keywords
            analysis = Regex.Replace(analysis, @"\b(idle|delay|bottleneck|underutilized)\b", $"{RED}$1{RESET}", RegexOptions.IgnoreCase);

            // Color positive keywords
            analysis = Regex.Replace(analysis, @"\b(completed|optimization|opportunity|efficient)\b", $"{GREEN}$1{RESET}", RegexOptions.IgnoreCase);

            // Color section dividers
            analysis = analysis.Replace("---", $"{BLUE}---{RESET}");

            return analysis;
        }

    }
}
