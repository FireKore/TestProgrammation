using UnityEngine;
using System.Collections;

public class EnnemySpawnerScript : MonoBehaviour {

    public GameObject enemy;
    public float spawnTime = 2f;
    public float minSpawnDistance = 2f;

    GameObject player;
    Terrain field;

	// Use this for initialization
	void Start ()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        field = GameObject.FindGameObjectWithTag("Terrain").GetComponent<Terrain>();
        InvokeRepeating("Spawn", spawnTime, spawnTime);
	}
	
	// Update is called once per frame
	void Update () {
	    
	}

    void Spawn()
    {
        Vector3 spawnPoint;
        do
        {
            spawnPoint = new Vector3(Random.Range(field.transform.position.x, field.transform.position.x + field.terrainData.size.x), 0, Random.Range(field.transform.position.z, field.transform.position.z + field.terrainData.size.z));
        }
        while (Vector3.Distance(player.transform.position, spawnPoint) <= minSpawnDistance);
        Instantiate(enemy, spawnPoint, new Quaternion());
    }
}
