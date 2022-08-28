using UnityEngine;
using System.Collections.Generic;

public class HelloWorld : MonoBehaviour
{
    public GameObject chunk;
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Hello World");

        int[,,] array = {{{1,1,1}, {1,1,0}, {1,1,1}}};

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                var chunkInstance = Instantiate(chunk, new Vector3(i * 3, -1, j * 3), Quaternion.identity);
                chunkInstance.GetComponent<VoxelGenerator>().setBlocks(array);
            }
        }
    }
}
