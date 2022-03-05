using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameRenderer : MonoBehaviour
{
    [SerializeField]
    private GameObject character;

    [SerializeField]
    private GameObject tile;

    [SerializeField]
    private GameObject food;

    private List<GameObject> entityObjs = new List<GameObject>{};

    public void Start()
    {
        for (int i = 0; i < GameModel.WIDTH; i++)
        {
            for (int j = 0; j < GameModel.HEIGHT; j++)
            {
                if (i==0 && j==0)
                    continue;
                var _tile = Instantiate(tile);
                _tile.transform.Translate(new Vector3(j, i, 0));
            }
        }
    }

    public void MyUpdate(List<GameModel.Entity> entities)
    {
        for (int i = 0; i < entities.Count; i++)
        {
            var entity = entities[i];
            if (entity.tags.Contains("Character"))
            {
                var _entity = i < entityObjs.Count ? entityObjs[i] : Instantiate(character);
                _entity.transform.position = new Vector3(entity.x, entity.y, 0);
                if (i - 1 < entityObjs.Count)
                    entityObjs.Add(_entity);
            }
            else if (entity.tags.Contains("Food"))
            {
                var _entity = i < entityObjs.Count ? entityObjs[i] : Instantiate(food);
                _entity.transform.position = new Vector3(entity.x, entity.y, 0);
                if (i - 1 < entityObjs.Count)
                    entityObjs.Add(_entity);
            }
            Debug.Log(entityObjs.ToString());
        }
    }
}
