using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    List<Tile> robSelectable = new List<Tile>();
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;

    void Start()
    {
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    public void InitAdjacencyLists()
    {
        // Creamos una matriz de adyacencia vacía del tamaño NumTiles x NumTiles
        int[,] adjacencyMatrix = new int[Constants.NumTiles, Constants.NumTiles];

        // Recorremos todas las casillas del tablero
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            // Calculamos la fila y la columna de la casilla actual
            int row = i / Constants.TilesPerRow;
            int col = i % Constants.TilesPerRow;

            // Casilla de arriba
            if (row > 0)
                adjacencyMatrix[i, i - Constants.TilesPerRow] = 1;

            // Casilla de abajo
            if (row < Constants.TilesPerRow - 1)
                adjacencyMatrix[i, i + Constants.TilesPerRow] = 1;

            // Casilla de la izquierda
            if (col > 0)
                adjacencyMatrix[i, i - 1] = 1;

            // Casilla de la derecha
            if (col < Constants.TilesPerRow - 1)
                adjacencyMatrix[i, i + 1] = 1;
        }

        // Recorremos la matriz de adyacencia para generar las listas de adyacencia de cada casilla
        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                // Si hay una conexión (valor 1) entre las casillas i y j
                if (adjacencyMatrix[i, j] == 1)
                {
                    // Añadimos j a la lista de adyacencia de la casilla i
                    tiles[i].adjacency.Add(j);
                }
            }
        }
    }


    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
        robSelectable.Clear();
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;
                break;
        }
    }

    public void ClickOnTile(int t)
    {
        clickedTile = t;

        switch (state)
        {
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected;
                }
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);
        // Generate a list of available tiles for the robber
        List<Tile> availableTiles = new List<Tile>();
        foreach (Tile tile in robSelectable)
        {
            if (tiles[clickedTile] != tile)
            {
                availableTiles.Add(tile);
            }
        }

        // Choose a random tile from the available tiles
        int randomIndex = Random.Range(0, availableTiles.Count);
        Tile newTile = availableTiles[randomIndex];

        robber.GetComponent<RobberMove>().MoveToTile(newTile);
        robber.GetComponent<RobberMove>().currentTile = newTile.numTile;
    }

        public void EndGame(bool end)
    {
        if (end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;

    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
        int indexcurrentTile;

        if (cop)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        int indexanothercop = cops[1 - clickedCop].GetComponent<CopMove>().currentTile;

        // Establecer el estado actual en true (rosa) porque se acaba de reiniciar
        tiles[indexcurrentTile].current = true;

        // Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        List<Tile> selectable = new List<Tile>();

        nodes.Enqueue(tiles[indexcurrentTile]);

        while (nodes.Count > 0)
        {
            Tile tile = nodes.Dequeue();

            if (selectable.Contains(tile))
                continue;

            if (tile != tiles[indexanothercop])
                selectable.Add(tile);

            foreach (var adjacentTileIndex in tile.adjacency)
            {
                if (tiles[adjacentTileIndex] != tiles[indexanothercop] && !selectable.Contains(tiles[adjacentTileIndex]))
                {
                    nodes.Enqueue(tiles[adjacentTileIndex]);

                    foreach (var adjacentAdjacentTileIndex in tiles[adjacentTileIndex].adjacency)
                    {
                        if (!selectable.Contains(tiles[adjacentAdjacentTileIndex]))
                            nodes.Enqueue(tiles[adjacentAdjacentTileIndex]);
                    }
                }
            }
        }

        if (cop)
        {
            foreach (var tile in selectable)
            {
                if (tile != tiles[indexcurrentTile])
                    tile.selectable = true;
            }
        }
        else
        {
            foreach (var tile in selectable)
            {
                if (tile != tiles[indexcurrentTile])
                {
                    tile.selectable = true;
                    robSelectable.Add(tile);
                }
            }
        }
    }
}
