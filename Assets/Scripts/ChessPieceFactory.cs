using UnityEngine;

public enum ChessPieceType { Pawn, Rook, Knight, Bishop, Queen, King }
public enum ChessPieceColor { White, Black }

public class ChessPieceInfo : MonoBehaviour
{
    public ChessPieceType type;
    public ChessPieceColor color;
    public int row;
    public int col;
}

public class ChessPieceFactory : MonoBehaviour
{
    public float squareSize = 1f;

    public Color whiteColor = new Color(0.95f, 0.95f, 0.9f);
    public Color blackColor = new Color(0.08f, 0.08f, 0.08f);

    public GameObject whitePawnPrefab;
    public GameObject whiteRookPrefab;
    public GameObject whiteKnightPrefab;
    public GameObject whiteBishopPrefab;
    public GameObject whiteQueenPrefab;
    public GameObject whiteKingPrefab;

    public GameObject blackPawnPrefab;
    public GameObject blackRookPrefab;
    public GameObject blackKnightPrefab;
    public GameObject blackBishopPrefab;
    public GameObject blackQueenPrefab;
    public GameObject blackKingPrefab;

    public float modelScaleMultiplier = 1f;
    public bool preservePrefabRotation = true;
    public float blackExtraYRotation = 0f;

    public bool addFallbackCollider = true;
    public Vector3 fallbackColliderSize = new Vector3(0.6f, 1f, 0.6f); 
    public Vector3 fallbackColliderCenter = new Vector3(0f, 0.5f, 0f); 
    private Material whiteMat;
    private Material blackMat;

    void Awake()
    {
        whiteMat = CreateMaterial(whiteColor);
        blackMat = CreateMaterial(blackColor);
    }

    public GameObject CreatePiece(ChessPieceType type, ChessPieceColor color, Vector3 localPos, int row, int col, Transform parent = null)
    {
        GameObject prefab = GetPrefabFor(type, color);

        GameObject piece = prefab != null
            ? CreateFromPrefab(prefab, type, color, localPos, parent)
            : CreateProcedural(type, color, localPos, parent);

        ChessPieceInfo info = piece.GetComponent<ChessPieceInfo>();
        if (info == null) info = piece.AddComponent<ChessPieceInfo>();
        info.type = type;
        info.color = color;
        info.row = row;
        info.col = col;

        return piece;
    }

    private GameObject GetPrefabFor(ChessPieceType type, ChessPieceColor color)
    {
        bool isWhite = color == ChessPieceColor.White;
        switch (type)
        {
            case ChessPieceType.Pawn: return isWhite ? whitePawnPrefab : blackPawnPrefab;
            case ChessPieceType.Rook: return isWhite ? whiteRookPrefab : blackRookPrefab;
            case ChessPieceType.Knight: return isWhite ? whiteKnightPrefab : blackKnightPrefab;
            case ChessPieceType.Bishop: return isWhite ? whiteBishopPrefab : blackBishopPrefab;
            case ChessPieceType.Queen: return isWhite ? whiteQueenPrefab : blackQueenPrefab;
            case ChessPieceType.King: return isWhite ? whiteKingPrefab : blackKingPrefab;
        }
        return null;
    }

    private GameObject CreateFromPrefab(GameObject prefab, ChessPieceType type, ChessPieceColor color, Vector3 localPos, Transform parent)
    {
        GameObject piece = Instantiate(prefab);
        piece.name = $"{color}_{type}";
        piece.transform.SetParent(parent != null ? parent : transform, false);
        piece.transform.localPosition = localPos;

        piece.transform.localRotation = preservePrefabRotation ? prefab.transform.localRotation : Quaternion.identity;
        if (color == ChessPieceColor.Black && blackExtraYRotation != 0f)
        {
            piece.transform.localRotation *= Quaternion.Euler(0f, blackExtraYRotation, 0f);
        }

        float scale = squareSize * modelScaleMultiplier;
        piece.transform.localScale = prefab.transform.localScale * scale;

        if (addFallbackCollider && piece.GetComponentInChildren<Collider>() == null)
        {
            BoxCollider col = piece.AddComponent<BoxCollider>();
            col.center = fallbackColliderCenter * squareSize / scale;
            col.size = fallbackColliderSize * squareSize / scale;
        }

        return piece;
    }

    private GameObject CreateProcedural(ChessPieceType type, ChessPieceColor color, Vector3 worldPosition, Transform parent)
    {
        if (whiteMat == null) whiteMat = CreateMaterial(whiteColor);
        if (blackMat == null) blackMat = CreateMaterial(blackColor);

        Material mat = color == ChessPieceColor.White ? whiteMat : blackMat;

        GameObject piece = new GameObject($"{color}_{type}_Procedural");
        piece.transform.SetParent(parent != null ? parent : transform, false);
        piece.transform.localPosition = worldPosition;

        switch (type)
        {
            case ChessPieceType.Pawn: BuildPawn(piece.transform, mat); break;
            case ChessPieceType.Rook: BuildRook(piece.transform, mat); break;
            case ChessPieceType.Knight: BuildKnight(piece.transform, mat); break;
            case ChessPieceType.Bishop: BuildBishop(piece.transform, mat); break;
            case ChessPieceType.Queen: BuildQueen(piece.transform, mat); break;
            case ChessPieceType.King: BuildKing(piece.transform, mat); break;
        }

        return piece;
    }

    private void BuildPawn(Transform root, Material mat)
    {
        float s = squareSize;
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.1f * s, 0), new Vector3(0.35f * s, 0.1f * s, 0.35f * s), mat);
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.35f * s, 0), new Vector3(0.22f * s, 0.25f * s, 0.22f * s), mat);
        AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.68f * s, 0), new Vector3(0.24f * s, 0.24f * s, 0.24f * s), mat);
    }
    private void BuildRook(Transform root, Material mat)
    {
        float s = squareSize;
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.1f * s, 0), new Vector3(0.4f * s, 0.1f * s, 0.4f * s), mat);
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.45f * s, 0), new Vector3(0.3f * s, 0.35f * s, 0.3f * s), mat);
        AddPart(root, PrimitiveType.Cube, new Vector3(0, 0.82f * s, 0), new Vector3(0.62f * s, 0.14f * s, 0.62f * s), mat);
        float r = 0.28f * s; float notchSize = 0.16f * s;
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            AddPart(root, PrimitiveType.Cube, new Vector3(Mathf.Cos(angle) * r, 0.92f * s, Mathf.Sin(angle) * r), new Vector3(notchSize, 0.12f * s, notchSize), mat);
        }
    }
    private void BuildKnight(Transform root, Material mat)
    {
        float s = squareSize;
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.1f * s, 0), new Vector3(0.38f * s, 0.1f * s, 0.38f * s), mat);
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.35f * s, 0), new Vector3(0.26f * s, 0.25f * s, 0.26f * s), mat);
        GameObject head = AddPart(root, PrimitiveType.Cube, new Vector3(0.05f * s, 0.62f * s, 0), new Vector3(0.24f * s, 0.34f * s, 0.2f * s), mat);
        head.transform.localRotation = Quaternion.Euler(0, 0, -20f);
        AddPart(root, PrimitiveType.Cube, new Vector3(0.24f * s, 0.72f * s, 0), new Vector3(0.2f * s, 0.14f * s, 0.16f * s), mat);
        GameObject ear = AddPart(root, PrimitiveType.Cube, new Vector3(-0.05f * s, 0.85f * s, 0.08f * s), new Vector3(0.08f * s, 0.16f * s, 0.08f * s), mat);
        ear.transform.localRotation = Quaternion.Euler(0, 0, 15f);
    }
    private void BuildBishop(Transform root, Material mat)
    {
        float s = squareSize;
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.1f * s, 0), new Vector3(0.36f * s, 0.1f * s, 0.36f * s), mat);
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.4f * s, 0), new Vector3(0.24f * s, 0.3f * s, 0.24f * s), mat);
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.68f * s, 0), new Vector3(0.16f * s, 0.14f * s, 0.16f * s), mat);
        AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.9f * s, 0), new Vector3(0.14f * s, 0.18f * s, 0.14f * s), mat);
        AddPart(root, PrimitiveType.Sphere, new Vector3(0, 1.05f * s, 0), new Vector3(0.06f * s, 0.06f * s, 0.06f * s), mat);
    }
    private void BuildQueen(Transform root, Material mat)
    {
        float s = squareSize;
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.1f * s, 0), new Vector3(0.42f * s, 0.1f * s, 0.42f * s), mat);
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.45f * s, 0), new Vector3(0.28f * s, 0.35f * s, 0.28f * s), mat);
        AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.85f * s, 0), new Vector3(0.34f * s, 0.28f * s, 0.34f * s), mat);
        float r = 0.22f * s; int spikes = 6;
        for (int i = 0; i < spikes; i++)
        {
            float angle = i * (360f / spikes) * Mathf.Deg2Rad;
            AddPart(root, PrimitiveType.Sphere, new Vector3(Mathf.Cos(angle) * r, 1.02f * s, Mathf.Sin(angle) * r), new Vector3(0.09f * s, 0.09f * s, 0.09f * s), mat);
        }
        AddPart(root, PrimitiveType.Sphere, new Vector3(0, 1.1f * s, 0), new Vector3(0.1f * s, 0.1f * s, 0.1f * s), mat);
    }
    private void BuildKing(Transform root, Material mat)
    {
        float s = squareSize;
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.1f * s, 0), new Vector3(0.42f * s, 0.1f * s, 0.42f * s), mat);
        AddPart(root, PrimitiveType.Cylinder, new Vector3(0, 0.48f * s, 0), new Vector3(0.28f * s, 0.38f * s, 0.28f * s), mat);
        AddPart(root, PrimitiveType.Sphere, new Vector3(0, 0.92f * s, 0), new Vector3(0.32f * s, 0.24f * s, 0.32f * s), mat);
        AddPart(root, PrimitiveType.Cube, new Vector3(0, 1.14f * s, 0), new Vector3(0.08f * s, 0.22f * s, 0.08f * s), mat);
        AddPart(root, PrimitiveType.Cube, new Vector3(0, 1.2f * s, 0), new Vector3(0.2f * s, 0.08f * s, 0.08f * s), mat);
    }

    private GameObject AddPart(Transform parent, PrimitiveType primitive, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPos;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = mat;
        Destroy(part.GetComponent<Collider>());
        return part;
    }

    private Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }
}