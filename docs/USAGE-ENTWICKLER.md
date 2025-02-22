# Nutzung der Ha-Py Virtuell App (Entwicklerdokumentation)

Willkommen bei der Entwicklerdokumentation für das Unity-Projekt "Ha-Py Virtuell". Diese Dokumentation bietet eine Anleitung zur Einrichtung, Entwicklung und zum Testen der App in Ihrer Entwicklungsumgebung.

## Projekt Setup

Nachdem Sie das Projekt geklont und in Unity geöffnet haben, folgen Sie diesen Schritten, um die Entwicklungsumgebung einzurichten:

### Abhängigkeiten

Stellen Sie sicher, dass alle externen Abhängigkeiten und Unity-Pakete installiert sind:

1. Öffnen Sie den Unity Package Manager unter `Window > Package Manager`.
2. Installieren Sie notwendige Pakete wie ARFoundation und andere AR-bezogene Tools, die für die AR-Funktionalität der App benötigt werden.

### Lokale Konfiguration

Konfigurieren Sie die lokalen Einstellungen:

- **Szenen laden**: Öffnen Sie die Hauptszene der App unter `Assets/Scenes/Main.unity`.
- **Konfigurationsdateien**: Überprüfen Sie die Konfigurationsdatei unter `Assets/Resources/config.json`, um Einstellungen wie Endpunkte für APIs oder andere Dienste anzupassen. Bitte fügen sie diese Datei hinzu, sofern sie noch nicht vorhanden ist. Diese Datei sollte die Authentifizierungsdaten enthalten, um die JSON-Dateien des Drupal-Backends abrufen zu können.</br >
config.json:
{
	"usrStage": "username",
	"pwStage": "password"
}

Die API nutzt eine Authentifizierung, um den Zugriff auf die Daten zu kontrollieren und sicherzustellen, dass nur autorisierte Nutzer Zugang zu den Ressourcen haben. 

Für weitere Fragen zur API kontaktieren Sie bitte [info@ha-py-virtuell.de](mailto:info@ha-py-virtuell.de).

## Entwicklung und Erweiterung

### Neue Features hinzufügen

Wenn Sie neue Features entwickeln:

1. Erstellen Sie einen neuen Branch von `main`, um Ihre Änderungen isoliert zu halten.
2. Implementieren Sie Ihr Feature im entsprechenden Bereich des Projekts. Wenn es sich um eine AR-Funktion handelt, arbeiten Sie innerhalb der Skripte unter `Assets/_Stadtrundgang-Hameln/Scripts`.
3. Fügen Sie notwendige Assets hinzu und stellen Sie sicher, dass diese korrekt in die Build-Konfiguration aufgenommen sind.

### Lokales Testing

Um neue Änderungen zu testen:

- **Play Mode**: Verwenden Sie den Unity Editor im Play Mode, um Ihre Änderungen interaktiv zu testen.
- **Unit Tests**: Führen Sie Unit Tests durch, um sicherzustellen, dass Ihre neuen Features wie erwartet funktionieren und keine bestehenden Funktionen beeinträchtigen.

### Dokumentation

Aktualisieren Sie die Dokumentation:

- **Code-Dokumentation**: Kommentieren Sie Ihren Code ausführlich, um die Funktionen und deren Nutzung zu erklären.
- **README.md**: Aktualisieren Sie die `README.md`-Datei, wenn große Änderungen oder neue Features hinzugefügt werden.

## Deployment

Folgen Sie diesen Schritten, um Änderungen zu deployen:

1. Stellen Sie sicher, dass alle Tests erfolgreich durchlaufen.
2. Führen Sie Ihre Änderungen in den main-Branch zusammen.
3. Verwenden Sie die Unity Build Einstellungen, um die App für die entsprechenden Plattformen zu bauen.

## Unterstützung

Bei Fragen oder Problemen während der Entwicklung können Sie:

- **Issue Tracker**: Verwenden Sie den GitHub Issue Tracker, um Probleme zu melden oder vorhandene zu überprüfen.
- **Community Forums**: Beteiligen Sie sich an Diskussionen auf Plattformen wie Stack Overflow oder Unity-Foren, um Unterstützung von der Community zu erhalten.

Wir hoffen, dass diese Dokumentation Ihnen hilft, effektiv am Ha-Py Virtuell Projekt mitzuwirken und Ihre Entwicklungserfahrung zu verbessern.
