<p align="center">More actions
  <img src="https://github.com/VladBelibou/FlowForge/blob/main/images/Light%20Mode%20Logo.png#gh-light-mode-only">
  <img src="https://github.com/VladBelibou/FlowForge/blob/main/images/Dark%20Mode%20Logo.png#gh-dark-mode-only">
</p>

# FlowForge

#### Diese Anwendung dient lediglich als Konzept
> Ein intelligentes System zur Produktionsplanung

## 🎯 Was ist FlowForge?

FlowForge ist ein KI-gestütztes Planungstool, das zur Optimierung von Produktionsabläufen gedacht ist. Es ermöglicht, Produktionsprozesse einfach per natürlicher Sprache zu steuern und flexibel anzupassen.

## 📊 Ein praktisches Beispiel

**Situation:** "Maschine 2 muss plötzlich gewartet werden."

**Eingabe:** "Maschine 2 ist ausgefallen, bitte Zeitplan umstellen."

**FlowForge reagiert:**
- Verschiebt die Aufträge auf andere Maschinen
- Sorgt für minimale Verzögerungen
- Informiert den Benutzer über alle Änderungen

### So sieht eine typische Antwort aus

```
Erklärung: "Aufgrund der Wartung an Maschine 2 wurden die Aufträge 
umgeplant. Auftrag 2 läuft jetzt auf Maschine 3, Auftrag 5 auf 
Maschine 1. Die Gesamtverzögerung beträgt nur 45 Minuten."

Geschätzte Fertigstellung: 13. Juni, 10:50 Uhr
Fortschritt: 40% abgeschlossen
Status: 2 von 5 Aufträgen fertig
```

<p align="center">
  <img src="https://github.com/VladBelibou/FlowForge/blob/main/demo/FlowForge-Demo.gif">
</p>

## 💡 Welches Problem löst FlowForge?

In der Produktion müssen täglich mehrere Entscheidungen getroffen werden:
- Priorisierung von Aufträgen
- Reaktion auf Maschinenausfälle
- Optimierung von Durchlaufzeiten

Normalerweise erfordert die Anpassung von Produktionsplänen technisches Know-how und viel Zeit.  Mit FlowForge lässt sich einfach in normaler Sprache eingeben, was benötigt wird.

## ⚡ Die wichtigsten Funktionen

### 1. **Einfache Bedienung mit natürlicher Sprache** 
Anstatt komplizierte Befehle einzugeben, kann einfach geschrieben werden:
- Maschine 2 muss gewartet werden, plane alles um
- Die Produktion muss für den minimalen Materialverbrauch optimiert werden

### 2. **Alles im Blick** 
- Sofortige Erkennung, wenn etwas nicht nach Plan läuft
- Bei Einsichten zeigen farbige Markierungen kritische Bereiche

### 3. **Intelligente Anpassungen** 
- Neue Zeitpläne werden bei Verzögerungen automatisch berechnet
- Optimierungsvorschläge basieren auf echten Produktionsdaten

##  🐧 Linux/WSL Setup

### 1. .NET 8.0 SDK installieren:
```bash
wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb &&
sudo dpkg -i packages-microsoft-prod.deb &&
rm packages-microsoft-prod.deb &&
sudo apt update && 
sudo apt install -y dotnet-sdk-8.0
```

### 2. Andere Abhängigkeiten
```bash
sudo apt install git &&
sudo apt install curl
sudo apt install jq
```

### 3. Repository klonen:
```bash
git clone https://github.com/VladBelibou/FlowForge.git
cd FlowForge
```

### 4. API-Einstellungen konfigurieren
```bash
# appsettings.json bearbeiten und OpenAI API-Schlüssel hinzufügen
nano appsettings.json
```

### 5. Anwendung starten:
```bash
dotnet run
```

##  🐧 Linux/WSL | API testen

### Neuen Zeitplan erstellen
```bash
curl -X POST http://localhost:5000/api/Scheduling/create -H "Content-Type: application/json" -d '{"schedulerName": "IhrName"}' | jq
```

### Aktuellen Zeitplan-Status abrufen
```bash
curl -X POST http://localhost:5000/api/Scheduling/status -H "Content-Type: application/json" -d '{}' | jq
```

### Element-Status aktualisieren (Beispiel: Element 1 früh beenden)
```bash
 curl -X PUT http://localhost:5000/api/Scheduling/status -H "Content-Type: application/json" -d '{"scheduleId": 1234, "itemId": 1, "status": 2}' | jq
```

### KI-gestütze Optimierung (benötigt API-Schlüssel)
```bash
curl -X POST http://localhost:5000/api/Scheduling/optimize -H "Content-Type: application/json" -d '{"
naturalLanguageRequest": "Optimiere diesen Zeitplan"}' | jq
```

### KI Erkenntnisse abrufen
```bash
curl http://localhost:5000/api/Scheduling/insights
```

### Zeitplan löschen
```bash
curl -X DELETE http://localhost:5000/api/Scheduling/1234
```

##  🪟 Windows Setup

### 1. .NET 8.0 SDK installieren:
- Von https://dotnet.microsoft.com/download herunterladen

### 2. Git installieren:
- Von https://git-scm.com/download/win herunterladen

### 3. Repository klonen:
```ps1
git clone https://github.com/VladBelibou/FlowForge.git
cd FlowForge
```

### 4. API-Einstellungen konfigurieren
```ps1
# appsettings.json im Editor öffnen und OpenAI API-Schlüssel hinzufügen
notepad appsettings.json
```

### 5. Anwendung starten:
```ps1
dotnet run
```

##  🪟 Windows | API testen

### Neuen Zeitplan erstellen
```ps1
(Invoke-RestMethod -Uri "http://localhost:5000/api/Scheduling/create" -Method Post -ContentType "application/json" -Body '{"schedulerName": "IhrName"}') | ConvertTo-Json
```

### Aktuellen Zeitplan-Status abrufen
```ps1
(Invoke-RestMethod -Uri "http://localhost:5000/api/Scheduling/status" -Method Post -ContentType "application/json" -Body '{}') | ConvertTo-Json
```

### Element-Status aktualisieren (Beispiel: Element 1 früh beenden)
```ps1
(Invoke-RestMethod -Uri "http://localhost:5000/api/Scheduling/status" -Method Put -ContentType "application/json" -Body '{"scheduleId": 1234, "itemId": 1, "status": 2}') | ConvertTo-Json
```

### KI-gestütze Optimierung (benötigt API-Schlüssel)
```ps1
(Invoke-RestMethod -Uri "http://localhost:5000/api/Scheduling/optimize" -Method Post -ContentType "application/json" -Body '{"naturalLanguageRequest": "Optimiere diesen Zeitplan"}') | ConvertTo-Json
```

### KI Erkenntnisse abrufen
```ps1
(Invoke-RestMethod -Uri "http://localhost:5000/api/Scheduling/insights")
```

### Zeitplan löschen
```ps1
Invoke-RestMethod -Uri "http://localhost:5000/api/Scheduling/1234" -Method Delete
```

##  📄 Lizenz

Dieses Projekt ist unter der MIT-Lizenz lizenziert - siehe die **LICENSE** Datei für Details.
