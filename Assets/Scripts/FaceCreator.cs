using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceCreator : MonoBehaviour
{
    /*
    public string path = "Assets/Resources/GameData/Faces"; // Папка для сохранения ScriptableObjects

    [SerializeField] private Sprite[] man_faces;
    [SerializeField] private Sprite[] woman_faces;

    [SerializeField] private Sprite[] man_faces_icons;
    [SerializeField] private Sprite[] woman_faces_icons;

    int price = 0;

    private void Start()
    {
        CreateFaces();
    }

    public void CreateFaces()
    {
        for (int i = 0; i < man_faces.Length; i++)
        {
            // Создаем новый Face
            Face face = ScriptableObject.CreateInstance<Face>();
            face.face = new FaceShell();
            face.id = i;
            face.price = price;
            face.icon = man_faces_icons[i];

            face.face.manFace = man_faces[i];
            face.face.womanFace = woman_faces[i];

            face.man_icon = man_faces_icons[i];
            face.woman_icon = woman_faces_icons[i];

            price += 50;

            // Создаем путь для сохранения
            string assetPath = $"{path}/Face{i}.asset";

            // Сохраняем ScriptableObject
            AssetDatabase.CreateAsset(face, assetPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Все лица созданы!");
    }
    */
}
