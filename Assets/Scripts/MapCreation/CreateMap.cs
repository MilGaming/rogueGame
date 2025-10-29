using UnityEngine;
using UnityEngine.Tilemaps;


public class CreateMap : MonoBehaviour

{
    Tilemap tilemapGround;
    Tilemap tilemapWall;

    [SerializeField] TileBase groundTile;
    [SerializeField] TileBase wallTile;

    [SerializeField] int sizeX;
    [SerializeField] int sizeY;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var list = GetComponentsInChildren<Tilemap>();
        foreach (var elem in list) {
            if (elem.name == "Ground") {
                tilemapGround = elem;
            }
            else {
                tilemapWall = elem;
            }
            Debug.Log((elem.name.ToString()));
        }
        GenerateMap();
        
    }

    void GenerateMap(){
        Vector3Int test = new Vector3Int(0, 0, 0);
        tilemapGround.ClearAllTiles();
        tilemapWall.ClearAllTiles();
        for (int x = 0; x < sizeX; x++) {
            for (int y = 0; y < sizeY;  y++) {
                test.x = x;
                test.y = y;
                if (x==0 || y==0 || x==(sizeX-1) || y==(sizeY-1)){
                    tilemapWall.SetTile(test, wallTile);
                } 
                else {
                    tilemapGround.SetTile(test, groundTile);
                }  
        }
        }
    }
}
