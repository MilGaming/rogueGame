# rogueGame

To create new maps go into the MapEliteTest.scene (in scenes) and give it the parameters you want in the grid object under the Map-Elite script. The rest should not be changed. 
To change which MAP-elite algorithm should be used, open up MapElite.cs (under MapCreation/MapElite) find the start function and out/in comment:
RunMapElitesGeometry();
MapArchiveExporter.ExportArchiveToJson(geoArchive.Values, "geoArchive_maps.json");
RunMapElitesEnemies();
MapArchiveExporter.ExportArchiveToJson(enemArchive.Values, "enemArchive_maps.json");
RunMapElitesFurnishing();
MapArchiveExporter.ExportArchiveToJson(furnArchive.Values, "furnArchive_maps.json");

For the hierarchical version.

For the combined version out/in comment:
RunMapElitesCombined();
MapArchiveExporter.ExportArchiveToJson(combinedArchive.Values, "combArchive_maps.json");

Once everything has been setup to preference just press play in the scene (MapEliteTest.scene).

To play through maps, go into the GamePlay.scne (in scenes). 
Add the archive(s) you want to use to the folder Assets/StreamingAssets.
Either furnArchive_maps or the combArchive_maps json files.
To select which one to use open up the LevelManager.cs under Assets/MapCreation.
Find the start function.
For combined maps out/in comment:
string path = Path.Combine(Application.streamingAssetsPath, "combArchive_maps.json");

For hierarchical amps out/in comment:
string path = Path.Combine(Application.streamingAssetsPath, "furnArchive_maps.json");

Once everything has been setup to preference just press play in the scene (GamePlay.scene).
