# API-Dokumentation für Ha-Py Virtuell

## Überblick

Diese Dokumentation beschreibt die Schnittstellen für die Ha-Py Virtuell API, die verwendet wird, um Daten zu Touren und Stationen dynamisch zu laden und zu verwalten.

## Basis-URL

Die API ist zugänglich über:
[https://hameln.stadtrundgang.dev.hob-by-horse.de/api/](https://hameln.stadtrundgang.dev.hob-by-horse.de/api/)


## Authentifizierung

Die API nutzt eine Authentifizierung, um den Zugriff auf die Daten zu kontrollieren und sicherzustellen, dass nur autorisierte Nutzer Zugang zu den Ressourcen haben. Dies gewährleistet, dass sensible Informationen geschützt bleiben und nur legitimierte Anfragen verarbeitet werden.

## Endpunkte

### Touren

#### Liste aller Touren
- **URL**: `/tours.json`
- **Methode**: `GET`
- **Beschreibung**: Ruft eine Liste aller verfügbaren Touren ab.
- **Antwort**:
```json
  [
    {
      "id": "rauchgaskegel",
      "imageURL": "https://hameln.stadtrundgang.dev.hob-by-horse.de/sites/default/files/2025-01/Grabung-hoch-2-kl-Text-2_1737489296.jpg",
      "title": "Auf den Spuren unserer Glasmacher",
      "subTitle": "Herzlich willkommen!",
      "description": "Diese Tour führt dich durch die historische Glashütte in Klein Süntel. Zwei Arten der Nutzung sind möglich: mit QR-Codes und ortsunabhängig.",
      "distance": "1,0 km",
      "duration": "ca. 0.75 h",
      "stations": ["glasmacher", "glashuette1", "glashuette2", "glashuette3", "glashuette4"]
    }
  ]
```


### Stationen

#### Details einer Station
- **URL**: /stations/{id}.json
- **Methode**: `GET`
URL-Parameter:
id: ID der Station
- **Beschreibung**: Ruft Details zu einer spezifischen Station ab.
- **Beispielantwort für id = glasmacher:**: 
```json
{
  "id": "glasmacher",
  "title": "Willkommen",
  "latitude": 52.167844,
  "longitude": 9.438546,
  "features": [
    {
      "id": "info",
      "title": "Geschichte wird lebendig",
      "description": "Hier erfahren Sie mehr über die historischen Aspekte der Region."
    }
  ]
}
```


## Fehlercodes
- **200 OK**: Die Anfrage war erfolgreich.
- **404 NOT FOUND**: Die angeforderte Ressource wurde nicht gefunden.
- **500 INTERNAL SERVER ERROR**: Allgemeiner Fehler auf dem Server.

## Nutzungslimit
Es gibt derzeit keine Beschränkungen für die Häufigkeit der API-Anfragen.

## Kontakt
Für weitere Fragen zur API kontaktieren Sie bitte [smartcity@hameln-pyrmont.de](mailto:smartcity@hameln-pyrmont.de).

