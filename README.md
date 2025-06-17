<p align="center">
  <img src="https://github.com/VladBelibou/FlowForge/blob/main/images/Light%20Mode%20Logo.png#gh-light-mode-only">
  <img src="https://github.com/VladBelibou/FlowForge/blob/main/images/Dark%20Mode%20Logo.png#gh-dark-mode-only">
</p>

# FlowForge

> Ein intelligentes System zur Produktionsplanung

## 🎯 Was ist FlowForge?

FlowForge ist ein KI-gestütztes Planungstool für die Produktion gedacht um Abläufe zu optimieren.  Das System ermöglicht es, Produktionsabläufe über natürliche Sprache zu steuern und anzupassen.

## 📊 Ein praktisches Beispiel

**Situation:** "Maschine 2 muss plötzlich gewartet werden."

**Eingabe:** "Maschine 2 ist ausgefallen, bitte Zeitplan umstellen."

**FlowForge reagiert:**
- Verschiebt die Aufträge auf andere Maschinen
- Sorgt für minimale Verzögerungen
- Informiert über alle Änderungen

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
- Farbige Markierungen zeigen kritische Bereiche

### 3. **Intelligente Anpassungen** 
- Bei Verzögerungen werden automatisch neue Zeitpläne berechnet
- Optimierungsvorschläge basieren auf echten Produktionsdaten

##  🐧 Linux/WSL Setup

### 1. .NET 8.0 SDK installieren:
```bash
wget https://packages.microsoft.com/config/debian/11/packages-microsoft-prod.deb -O packages-microsoft-prod.deb &&
sudo dpkg -i packages-microsoft-prod.deb &&
rm packages-microsoft-prod.deb sudo apt update &&
sudo apt install -y dotnet-sdk-8.0
```

### 2. Für JSON-Formatierung jq installieren (optional aber empfohlen):
```bash
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
curl http://localhost:5000/api/Scheduling/insights | jq
```

##  📄 Lizenz

Dieses Projekt ist unter der MIT-Lizenz lizenziert - siehe die **LICENSE** Datei für Details.
