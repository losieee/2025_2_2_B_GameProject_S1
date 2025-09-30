using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal.VR;
using UnityEngine;

public class SimpleDungeon : MonoBehaviour
{
    [Header("���� ����")]

    public int roomCount = 8;
    public int minSize = 4;
    public int maxSize = 8;

    [Header("������ ����")]
    public bool spawnEnemies = true;
    public bool spawnTreasures = true;
    public int enemiesPerRoom = 2;

    private Dictionary<Vector2Int, Room> rooms = new Dictionary<Vector2Int, Room>();
    private HashSet<Vector2Int> floors = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> walls = new HashSet<Vector2Int>();

    // Start is called before the first frame update
    void Start()
    {
        Generate();
    }

    // Update is called once per frame
    void Update()   
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Clear();
            Generate();

        }
    }

    public void Generate()
    {
        //�� �������� ��Ģ������ �����.
        CreateRooms();

        //��� �� ���̸� ������ �����Ѵ�
        ConnectRooms();

        //�ٴ� �ֺ� Ÿ�Ͽ� ���� �ڵ� ��ġ�Ѵ�
        CreateWalls();

        //���� ����Ƽ �󿡼� ť��� Ÿ���� �׸���
        Render();

        //�� Ÿ�Կ� ���� ��/������ ��ġ�Ѵ�.
        SpawnObjects();
    }


    //���� �� 1�� ����, �������� ���� �� ��ó(��/��/��/��)�� �������� �ΰ� �õ�
    //������ ���� ���� ���������� ����, �Ϲ� �� �Ϻθ� ���������� ��ȯ
    void CreateRooms()
    {
        //���� �� : ������(0.0)�� ��ġ
        Vector2Int pos = Vector2Int.zero;
        int size = Random.Range(minSize, maxSize);
        AddRoom(pos, size, RoomType.Start);

        //������ �� ���� �õ�
        for(int i = 0; i < roomCount; i++)
        {
            var roomList = new List<Room>(rooms.Values);
            Room baseRoom = roomList[Random.Range(0, roomList.Count)];

            Vector2Int[] dirs =
            {
                Vector2Int.up * 6, Vector2Int.down * 6, Vector2Int.left * 6, Vector2Int.right * 6

            };

            foreach(var dir in dirs)
            {
                Vector2Int newPos = baseRoom.centor + dir;
                int newSize = Random.Range(minSize, maxSize);
                RoomType type = (i == roomCount - 1) ? RoomType.Boss : RoomType.Normal;
                if (AddRoom(newPos, newSize, type)) break;
            }
        }



        int treasureCount = Mathf.Max(1, roomCount / 4);
        var normalRooms = new List<Room>();

        foreach(var room in rooms.Values)
        {
            if(room.type == RoomType.Normal)
                normalRooms.Add(room);
        }

        for(int I = 0; I < treasureCount && normalRooms.Count > 0; I++)
        {
            int idx = Random.Range(0,normalRooms.Count);
            normalRooms[idx].type = RoomType.Treasure;
            normalRooms.RemoveAt(idx);
        }
    }



    bool AddRoom(Vector2Int center, int size, RoomType type)
    {
        //1. ��ħ �˻�
        for(int x = -size / 2; x < size / 2; x++)
        {
            for(int y = -size / 2; y < size / 2; y++)
            {
                Vector2Int tile = center + new Vector2Int(x, y);
                if (floors.Contains(tile))
                    return false;
            }
        }
        //2. �� ��Ÿ������ ���
        Room room = new Room(center, size, type);
        rooms[center] = room;

        //3. �� ������ floors�� ä���.
        for (int x = -size / 2; x < size / 2; x++)
        {
            for (int y = -size / 2; y < size / 2; y++)
            {
                floors.Add(center + new Vector2Int(x, y));
            }
        }
        return true;

    }

    //��� ���� ���� ������ ����
    void ConnectRooms()
    {
        var roomList = new List<Room>(rooms.Values);

        for(int i = 0; i < roomList.Count - 1; i++)
        {
            CreateCorridor(roomList[i].centor, roomList[i + 1].centor);
        }
    }

    //�� ��ǥ ���̸� x�� -> y�� ������ ���� ������ �Ǵ�
    //���� ġ�� L�� ����� ���´�

    void CreateCorridor(Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;

        //x�� ���� : start.x -> end.x�� ��ĭ�� �̵��ϸ� �ٴ� Ÿ�� �߰�
        while (current.x != end.x)
        {
            floors.Add(current);
            current.x += (end.x > current.x) ? 1 : -1;
        }
        //y�� ���� :  x�� ������ �� start.y -> end.y�� ��ĭ�� �̵�
        while (current.y != end.y)
        {
            floors.Add(current);
            current.y += (end.y > current.y) ? 1 : -1;
        }

        floors.Add(end);  //������ �������� �ٴ�ó��

    }

    //�ٴ� �ֺ��� 8������ ��ĵ�Ͽ�, �ٴ��� �ƴ� ĭ�� walls�� ä���
    void CreateWalls()
    {
        Vector2Int[] dirs =
            {
                Vector2Int.up, Vector2Int.down , Vector2Int.left, Vector2Int.right,
                new Vector2Int(1,1), new Vector2Int(1,-1), new Vector2Int(-1,1), new Vector2Int(-1, -1)
            };

        foreach(var floor in floors)
        {
            foreach(var dir in dirs)
            {
                Vector2Int wallPos = floor + dir;
                if(!floors.Contains(wallPos))
                {
                    walls.Add(wallPos);
                }
            }
        }
    }
    //Ÿ���� ����Ƽ ������Ʈ�� ������
    //�ٴ� : cube (0.1), �� cube (1), ��  �� ����
    void Render()
    {
        //�ٴ� Ÿ�� ������
        foreach(var pos in floors)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(pos.x, 0, pos.y);  //y = 0 ��鿡 ��ġ
            cube.transform.localScale = new Vector3 (1f, 0.1f , 1f);
            cube.transform.SetParent(transform);

            Room room = GetRoom(pos);
            if(room != null )
            {
                cube.GetComponent<Renderer>().material.color = room.GetColor();
            }
            else
            {
                cube.GetComponent<Renderer>().material.color = Color.white;
            }
        }

        //�� Ÿ�� ������
        foreach(var pos in walls)
        {
          GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
            cube.transform.position = new Vector3(pos.x, 0.5f, pos.y);
            cube.transform.SetParent (transform);
            cube.GetComponent<Renderer>().material.color = Color.black;
        }
    }
    //� �ٴ� ��ǥ�� "��� ��"�� ���ϴ��� ������
    Room GetRoom(Vector2Int pos)
    {
        foreach (var room in rooms.Values)
        {
            int halfSize = room.size / 2;
            if (Mathf.Abs(pos.x - room.centor.x) < halfSize && Mathf.Abs(pos.y - room.centor.y) < halfSize)
            {
                return room;
            }
        }
        return null;
    }

    void SpawnObjects()
    {
        foreach(var room in rooms.Values)
        {
            switch (room.type)
            {
                case RoomType.Start:
                    break;
                case RoomType.Normal:
                    if(spawnEnemies)
                        SpawnEnemiesInRoom(room);
                    break;
                case RoomType.Treasure:
                    if(spawnTreasures)
                        SpawnTreasureInRoom(room);
                    break;
                case RoomType.Boss:
                    if(spawnEnemies)
                        SpawnBossInRoom(room);
                    break;
            }
        }
    }

    Vector3 GetRandomPositionInRoom(Room room)
    {
        float halfSize = room.size / 2f - 1f;
        float randomX = room.centor.x + Random.Range(-halfSize, halfSize);
        float randomZ = room.centor.y + Random.Range(-halfSize, halfSize);

        return new Vector3 (randomX, 0.5f, randomZ);
    }

    void CreateEnemy(Vector3 position)
    {
        GameObject enemy = GameObject.CreatePrimitive (PrimitiveType.Sphere);
        enemy.transform.position = position;
        enemy.transform.localScale = Vector3.one * 0.8f;
        enemy.transform.SetParent (transform);
        enemy.name = "Enemy";
        enemy.GetComponent<Renderer>().material.color = Color.red;
    }
    void CreateBoss(Vector3 position)
    {
        GameObject boss = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boss.transform.position = position;
        boss.transform.localScale = Vector3.one * 2f;
        boss.transform.SetParent(transform);
        boss.name = "Boss";
        boss.GetComponent<Renderer>().material.color = Color.cyan;
    }

    void CreateTreasure(Vector3 position)
    {
        GameObject boss = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boss.transform.position = position;
        boss.transform.localScale = Vector3.one * 0.8f;
        boss.transform.SetParent(transform);
        boss.name = "Treasure";
        boss.GetComponent<Renderer>().material.color = Color.black;
    }


    void SpawnEnemiesInRoom(Room room)
    {
        for (int i = 0; i < enemiesPerRoom; i++)
        {
            Vector3 spawnPos = GetRandomPositionInRoom (room);
            CreateEnemy (spawnPos);
        }
    }

    void SpawnBossInRoom(Room room)
    {
        Vector3 spawnPos = new Vector3(room.centor.x, 1f, room.centor.y);
        CreateBoss(spawnPos);
    }
    void SpawnTreasureInRoom(Room room)
    {
        Vector3 spawnPos = new Vector3(room.centor.x, 1f, room.centor.y);
        CreateTreasure(spawnPos);
    }

    void Clear()
    {
        rooms.Clear();
        floors.Clear();
        walls.Clear();

        foreach(Transform child in  transform)
        {
            Destroy(child.gameObject);
        }
    }

}
