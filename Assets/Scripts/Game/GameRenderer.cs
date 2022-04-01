using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

public class GameRenderer : MonoBehaviour
{
    [SerializeField]
    private GameObject character;

    [SerializeField]
    private GameObject tile;

    [SerializeField]
    private GameObject food;

    private Dictionary<int, GameObject> entityObjs = new Dictionary<int, GameObject>{};

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

    public void MyUpdate(List<GameModel.Entity> entities, List<GameModel.RenderActionPair> actionList)
    {
        foreach (var renderActionPair in actionList) {
            var id = renderActionPair.ID;
            var action = renderActionPair.renderAction;
            var entity = entities[id];
                //prints each action taken on each frame
            //Debug.Log(action);
                //adds model to the render
            if (action == GameModel.RenderAction.Add) {
                if (entity.tags.Contains("Character")) {
                    entityObjs.Add(entity.ID, Instantiate(character));
                } else if (entity.tags.Contains("Food")) {
                    entityObjs.Add(entity.ID, Instantiate(food));
                }
                    //updates entity position on the render
                entityObjs[entity.ID].transform.position = new Vector3(entity.x, 0, entity.y); 
                    //removes entity from the render
            } else if (action == GameModel.RenderAction.Remove || action == GameModel.RenderAction.Eaten) {
                Destroy(entityObjs[entity.ID]);
                entityObjs.Remove(entity.ID);
                break;
                    //updates entity position on the render
            } else if (action == GameModel.RenderAction.Move) {

                var _entity = entityObjs[entity.ID];

                //UnityEngine.Debug.Log(_entity.GetComponent<AIController>().destination);
                var _destination = _entity.GetComponent<AIController>().destination;
                _entity.transform.position = new Vector3(_destination.x, 0, _destination.y);
                _entity.GetComponent<AIController>().destination = new Vector2(entity.x, entity.y);
                

                //UnityEngine.Debug.Log(_entity.GetComponent<AIController>().destination);
                //UnityEngine.Debug.Log(_entity.transform.position);
            }
        }
    }
}
