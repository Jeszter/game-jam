using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildFoodItems
{
    [MenuItem("Game/Build Food Items in Kitchen")]
    public static void Execute()
    {
        // Позиции относительно dinner_table (центр стола top)
        // Table top ~ Y = 192.99881, range X in 787.79..789.49, Z in -1.67..0.60
        var houseGO = GameObject.Find("House");
        if (houseGO == null)
        {
            Debug.LogError("House not found!");
            return;
        }

        // Создаём родителя House/Food
        Transform foodParent = houseGO.transform.Find("Food");
        if (foodParent == null)
        {
            var foodGO = new GameObject("Food");
            foodGO.transform.SetParent(houseGO.transform, true);
            foodGO.transform.position = Vector3.zero;
            foodParent = foodGO.transform;
        }

        // Удалим старые если были
        for (int i = foodParent.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(foodParent.GetChild(i).gameObject);

        // Обеденный стол top
        // Мы хотим чтобы еда стояла на столе. Позиции выберем в мировых координатах.
        // Центр: 788.645, 192.99881, -0.534
        Vector3 tableTop = new Vector3(788.645f, 193.05f, -0.534f);

        CreateFood(foodParent, "Pizza",  tableTop + new Vector3(-0.35f, 0f, -0.55f), new Color(0.95f, 0.6f, 0.2f),  FoodShape.Disk,    40f, 1.2f);
        CreateFood(foodParent, "Burger", tableTop + new Vector3( 0.35f, 0f, -0.55f), new Color(0.7f, 0.45f, 0.25f), FoodShape.Stack,  35f, 1.0f);
        CreateFood(foodParent, "Soda",   tableTop + new Vector3( 0.55f, 0f,  0.35f), new Color(0.9f, 0.1f, 0.15f),  FoodShape.Can,    15f, 0.6f);
        CreateFood(foodParent, "Apple",  tableTop + new Vector3(-0.55f, 0f,  0.35f), new Color(0.9f, 0.15f, 0.15f), FoodShape.Sphere, 25f, 0.4f);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Food items built under House/Food");
    }

    enum FoodShape { Sphere, Disk, Stack, Can }

    static void CreateFood(Transform parent, string name, Vector3 worldPos, Color color, FoodShape shape, float hunger, float dopaMult)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, true);
        root.transform.position = worldPos;

        var shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;

        GameObject mainVis = null;

        switch (shape)
        {
            case FoodShape.Sphere:
                {
                    mainVis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    mainVis.name = "Vis";
                    mainVis.transform.SetParent(root.transform, false);
                    mainVis.transform.localScale = Vector3.one * 0.18f;
                    mainVis.transform.localPosition = new Vector3(0, 0.09f, 0);
                }
                break;
            case FoodShape.Disk:
                {
                    mainVis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    mainVis.name = "Vis";
                    mainVis.transform.SetParent(root.transform, false);
                    mainVis.transform.localScale = new Vector3(0.35f, 0.03f, 0.35f);
                    mainVis.transform.localPosition = new Vector3(0, 0.03f, 0);
                    // топпинги
                    for (int i = 0; i < 5; i++)
                    {
                        var t = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        t.transform.SetParent(root.transform, false);
                        float a = i * Mathf.PI * 2f / 5f;
                        t.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.18f, 0.07f, Mathf.Sin(a) * 0.18f);
                        t.transform.localScale = Vector3.one * 0.06f;
                        var r = t.GetComponent<Renderer>();
                        var m = new Material(shader);
                        var topColor = new Color(0.8f, 0.15f, 0.15f);
                        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", topColor);
                        m.color = topColor;
                        r.sharedMaterial = m;
                        Object.DestroyImmediate(t.GetComponent<Collider>());
                    }
                }
                break;
            case FoodShape.Stack:
                {
                    mainVis = new GameObject("Vis");
                    mainVis.transform.SetParent(root.transform, false);
                    // bun bottom
                    var b0 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    b0.transform.SetParent(mainVis.transform, false);
                    b0.transform.localScale = new Vector3(0.18f, 0.025f, 0.18f);
                    b0.transform.localPosition = new Vector3(0, 0.025f, 0);
                    SetMat(b0, shader, new Color(0.85f, 0.6f, 0.3f));
                    Object.DestroyImmediate(b0.GetComponent<Collider>());
                    // patty
                    var p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    p.transform.SetParent(mainVis.transform, false);
                    p.transform.localScale = new Vector3(0.19f, 0.02f, 0.19f);
                    p.transform.localPosition = new Vector3(0, 0.07f, 0);
                    SetMat(p, shader, new Color(0.35f, 0.15f, 0.07f));
                    Object.DestroyImmediate(p.GetComponent<Collider>());
                    // bun top
                    var b1 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    b1.transform.SetParent(mainVis.transform, false);
                    b1.transform.localScale = new Vector3(0.18f, 0.09f, 0.18f);
                    b1.transform.localPosition = new Vector3(0, 0.1f, 0);
                    SetMat(b1, shader, new Color(0.85f, 0.6f, 0.3f));
                    Object.DestroyImmediate(b1.GetComponent<Collider>());
                }
                break;
            case FoodShape.Can:
                {
                    mainVis = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    mainVis.name = "Vis";
                    mainVis.transform.SetParent(root.transform, false);
                    mainVis.transform.localScale = new Vector3(0.1f, 0.13f, 0.1f);
                    mainVis.transform.localPosition = new Vector3(0, 0.13f, 0);
                }
                break;
        }

        if (mainVis != null)
        {
            var rend = mainVis.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
            var col = mainVis.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
        }

        // Box collider на root (для рэйкаста подсказки F)
        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, 0.12f, 0);
        bc.size = new Vector3(0.4f, 0.25f, 0.4f);

        // Тэг FoodItem
        var food = root.AddComponent<FoodItem>();
        food.foodName = name;
        food.hungerRestore = hunger;
        food.dopamineMultiplier = dopaMult;
    }

    static void SetMat(GameObject go, Shader shader, Color c)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var m = new Material(shader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c;
        r.sharedMaterial = m;
    }
}
