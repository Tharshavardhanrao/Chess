using UnityEngine;

public class ChessBoardGenerator : MonoBehaviour
{
    public int boardSize = 8;
    public float squareSize = 1f;
    public float tileThickness = 0.2f;

    public Color lightColor = new Color(0.93f, 0.87f, 0.73f);
    public Color darkColor = new Color(0.35f, 0.22f, 0.14f);

    public bool addBorder = true;
    public float borderThickness = 0.3f;
    public Color borderColor = new Color(0.15f, 0.1f, 0.05f);

    private GameObject boardParent;

    void Start()
    {
        GenerateBoard();
    }

    [ContextMenu("Generate Board")]
    public void GenerateBoard()
    {
        ClearBoard();

        boardParent = new GameObject("ChessBoard_Generated");
        boardParent.transform.SetParent(transform, false);

        Material lightMat = CreateMaterial(lightColor);
        Material darkMat = CreateMaterial(darkColor);

        float boardWorldSize = boardSize * squareSize;
        float halfBoard = boardWorldSize / 2f;

        for (int row = 0; row < boardSize; row++)
        {
            for (int col = 0; col < boardSize; col++)
            {
                bool isLight = (row + col) % 2 == 0;
                CreateSquare(row, col, isLight ? lightMat : darkMat, halfBoard);
            }
        }

        if (addBorder)
        {
            CreateBorder(boardWorldSize, halfBoard);
        }
    }

    private void CreateSquare(int row, int col, Material mat, float halfBoard)
    {
        GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tile.name = $"Tile_{row}_{col}";
        tile.transform.SetParent(boardParent.transform, false);

        float x = col * squareSize - halfBoard + squareSize / 2f;
        float z = row * squareSize - halfBoard + squareSize / 2f;

        tile.transform.localPosition = new Vector3(x, 0f, z);
        tile.transform.localScale = new Vector3(squareSize, tileThickness, squareSize);

        tile.GetComponent<Renderer>().sharedMaterial = mat;

        tile.tag = "Untagged";
        var info = tile.AddComponent<ChessSquareInfo>();
        info.row = row;
        info.col = col;
        info.algebraic = ToAlgebraic(row, col);
    }

    private void CreateBorder(float boardWorldSize, float halfBoard)
    {
        Material borderMat = CreateMaterial(borderColor);
        float outerHalf = halfBoard + borderThickness;

        CreateBorderSlab("Border_Back", new Vector3(0, 0, halfBoard + borderThickness / 2f),
            new Vector3(boardWorldSize + borderThickness * 2f, tileThickness, borderThickness), borderMat);

        CreateBorderSlab("Border_Front", new Vector3(0, 0, -halfBoard - borderThickness / 2f),
            new Vector3(boardWorldSize + borderThickness * 2f, tileThickness, borderThickness), borderMat);

        CreateBorderSlab("Border_Left", new Vector3(-halfBoard - borderThickness / 2f, 0, 0),
            new Vector3(borderThickness, tileThickness, boardWorldSize), borderMat);

        CreateBorderSlab("Border_Right", new Vector3(halfBoard + borderThickness / 2f, 0, 0),
            new Vector3(borderThickness, tileThickness, boardWorldSize), borderMat);
    }

    private void CreateBorderSlab(string name, Vector3 localPos, Vector3 scale, Material mat)
    {
        GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slab.name = name;
        slab.transform.SetParent(boardParent.transform, false);
        slab.transform.localPosition = localPos;
        slab.transform.localScale = scale;
        slab.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    private string ToAlgebraic(int row, int col)
    {
        char file = (char)('a' + col);
        int rank = row + 1;
        return $"{file}{rank}";
    }

    private void ClearBoard()
    {
        Transform existing = transform.Find("ChessBoard_Generated");
        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }   
    }
}

public class ChessSquareInfo : MonoBehaviour
{
    public int row;
    public int col;
    public string algebraic;
}