# Setup

- Unity Hub installieren: https://unity.com/download
- Unity installieren  
  Alle Versionen 6000.0.xxxx sollten funktionieren.
  Unity wird in der Unity Hub installiert. Dies benötigt möglicherweise einen kostenfreien Account.
- Projekt herunterladen: https://github.com/GaviTSRA/Seminarfach/archive/refs/heads/main.zip
- Projekt mit Unity Hub öffnen

# Nutzung des Projektes

Alle für die Präsentation genutzten Scenen befinden sich im Projektexplorer im Scenes Ordner. Von dort können diese geöffnet werden.  
Der Programmcode befindet sich im Scripts Ordner.  
Alle Modelle befinden sich im Resources/Models Ordner.

Wenn eine Scene geöffnet ist, können verschiedene Werte der Simulation verändert werden. Diese finden sich an 3 verschiedenen Orten:

- In der Hierarchy: Das Universe-Objekt. Dieses enthält Werte für die Vorschau und die Geschwindigkeit der Simulation
- Die Werte der einzelnen Objekte. Diese finden sich unter dem Universe-Objekt. Diese enthalten Daten wie die Position oder Geschwindigkeit.
- Generelle konstanten des Universums. Diese können im Script Universe.cs bearbeitet werden.

Um die Simulation zu starten kann der Play-Knopft oben mittig gedrückt werden. Das Ergebnis kann in der Scene-View angesehen werden.
