using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    public GameObject player;
    [Header("Farming")] // farming variables
    
    private Grid grid;
    private Camera cam;

    Dictionary<Vector3Int, int> activeTiles = new Dictionary<Vector3Int, int>();
    public Tilemap interactableMap;
    
    [Header("Tiles")]

    [SerializeField] private Tile hiddenInteractable;
    [SerializeField] private Tile tilled;
    [SerializeField] private Tile watered;
    [SerializeField] private Tile stage1Grow;
    [SerializeField] private Tile stage2Grow;
    [SerializeField] private Tile stage3Grow;
    [SerializeField] private Tile stage4Grow;

    [Header("Time")]

    public GameObject dayNightLighting;
    public DayNightLighting dayNightTime;

    [Header("Vendor")]
    public Volume ppv;
    public Camera vendorCam;
    public Light2D globalLight;

    // vendor scene player spotlights
    public Light2D playerSpotlight1;
    public Light2D playerSpotlight2;

    [Header("UI")]
    public UIManager ui;

    [Header("Misc")]

    public int money = 0;
    public Item Carrot;
    public InventoryManager inventoryManager;
    public GameObject endDayMenu;

    // indicates when a player is in a menu
    public bool inMenu = false;

    // variable to change sorting order of roof when player collides with door
    public bool inHouse = false;

    private void Start() {
        // sets cam to main camera, finds the grid, and disables the vendor camera
        cam = Camera.main;
        grid = GameObject.Find("FarmingSpace").GetComponent<Grid>();
        vendorCam.enabled = false;

        // disables spotlights for vendor scene
        playerSpotlight1.enabled = false;
        playerSpotlight2.enabled = false;

        // interactable white tiles indicate which spaces can be interacted with, but are hidden from the player on startup
        foreach(var position in interactableMap.cellBounds.allPositionsWithin) {
            TileBase tile = interactableMap.GetTile(position);
            if (tile != null && tile.name == "Interactable_Visible") {
                interactableMap.SetTile(position, hiddenInteractable);
            }
        }

        dayNightTime = dayNightLighting.GetComponent<DayNightLighting>();
        inventoryManager = FindObjectOfType<InventoryManager>();
    }

    // sets inMenu variable to true or false based on function input
    public void MenuState(bool state) {
        inMenu = state;
    }

    // checks if a tile can be interacted with
    public bool isInteractable(Vector3Int position) {
        
        TileBase tile = interactableMap.GetTile(position);
        // checks if the tile is null, as interactable spaces are just set to transparent pngs
        if (tile != null) {
            if (tile.name == "Interactable") {
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }

    }
    
    // checks if the carrot's stage is the highest stage (therefore is harvestable)
    public bool isHarvestable(Vector3Int position) {
        TileBase tile = interactableMap.GetTile(position);
        if (tile != null) {
            if (tile.name == "carrotFarmingTiles_5") {
                return true;
            } else {
                return false;
            }
        } else {
            return false;
        }
    }

    
   // harvests tile at position if harvestable
    public void harvest(Vector3Int position) {
        TileBase tile = interactableMap.GetTile(position);
        if (isHarvestable(position)) {
            inventoryManager.AddItem(Carrot);
            removeTile(position);
        } 
    }
    
    // sets up the vendor area using the fade to/from black effect
    IEnumerator vendorSet() {
        ui.fadeToBlack(true);
        yield return new WaitForSeconds(0.5f);
        globalLight.intensity = 0.16f;
        ppv.enabled = false;
        cam.enabled = false;
        vendorCam.enabled = true;
        player.transform.position = new Vector3(-30, -3.5f, 0);

        playerSpotlight1.enabled = true;
        playerSpotlight2.enabled = true;
        ui.fadeToBlack(false);
    }

    // sets up the farm using the fade to/from black effect
    IEnumerator farmSet() {
        ui.fadeToBlack(true);
        yield return new WaitForSeconds(0.5f);
        globalLight.intensity = 1;
        ppv.enabled = true;
        cam.enabled = true;
        vendorCam.enabled = false;
        player.transform.position = new Vector3(-7.5f, 19.5f, 0);

        playerSpotlight1.enabled = false;
        playerSpotlight2.enabled = false;
        ui.fadeToBlack(false);
    }

    // fade to black effect for sleeping
    IEnumerator sleepFade() {
        ui.fadeToBlack(true);
        yield return new WaitForSeconds(0.5f);
        growCrops();
        // change time to 8am the next day
        dayNightTime.ChangeTime(0, 0, 6, 1, true, false);
        ui.fadeToBlack(false);
    }

    public void SleepSequence() {
        StartCoroutine(sleepFade());
    }

    public void AddMoney(int money) {
        this.money += money;
    }

    public bool RemoveMoney(int money) {
        if (this.money - money < 0) {
            return false;
        } else {
            this.money -= money;
            return true;
        }
    }

    public Vector3Int mouseToTilePosition() {
        
        Vector3 point = new Vector3();
        Vector3 pointPos = new Vector3();

        // gets mouse position
        pointPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, cam.nearClipPlane);

        // converts mouse position to world position
        point = cam.ScreenToWorldPoint(pointPos);
        Vector3Int cellPosition = grid.WorldToCell(point);

        return cellPosition;
    }

    public void removeTile(Vector3Int cellPosition) {
        activeTiles.Remove(cellPosition);
        interactableMap.SetTile(cellPosition, hiddenInteractable);
    }


    public void addTile(Vector3Int cellPosition, string tileName) {
        TileBase tile = interactableMap.GetTile(cellPosition);

        switch(tileName) {
            
            case "tilled":
                if(activeTiles.Count == 0) {
                    activeTiles.Add(cellPosition, 1);
                    interactableMap.SetTile(cellPosition, tilled);

                } else if (!activeTiles.ContainsKey(cellPosition)){
                    activeTiles.Add(cellPosition, 1);
                    interactableMap.SetTile(cellPosition, tilled);
                } 

                break;

            case "watered":

                if (tile != null && tile.name == "carrotFarmingTiles_0" && activeTiles.ContainsKey(cellPosition)) {
                    activeTiles[cellPosition] = 2;
                    interactableMap.SetTile(cellPosition, watered);
                }

                break;
            
            case "stage1Grow":
    
                if (tile != null && tile.name == "carrotFarmingTiles_1" && activeTiles.ContainsKey(cellPosition)) {
                    activeTiles[cellPosition] = 3;
                    interactableMap.SetTile(cellPosition, stage1Grow);
                    // removes carrot seed from inventory after planting
                    inventoryManager.GetSelectedItem(true);
                }
    
                break;
        }    
    }

    public void VendorSetup() {
        StartCoroutine(vendorSet());
    } 

    public void FarmSetup() {
        StartCoroutine(farmSet());
    }


    public void growCrops() {
        foreach(var position in interactableMap.cellBounds.allPositionsWithin) {
            TileBase tile = interactableMap.GetTile(position);

            if (tile != null) {
                switch(tile.name) {
                    case "carrotFarmingTiles_2":
                        interactableMap.SetTile(position, stage2Grow);
                        break;
                    case "carrotFarmingTiles_3":
                        interactableMap.SetTile(position, stage3Grow);
                        break;
                    case "carrotFarmingTiles_4":
                        interactableMap.SetTile(position, stage4Grow);
                        break;   
                }
            }
        }
    }
}
