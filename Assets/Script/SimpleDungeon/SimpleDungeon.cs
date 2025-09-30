using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal.VR;
using UnityEngine;

public class SimpleDungeon : MonoBehaviour
{
    [Header("던전 설정")]

    public int roomCount = 8;
    public int minSize = 4;
    public int maxSize = 8;

    [Header("스포너 설정")]
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
        //방 여러개를 규칙적으로 만든다.
        CreateRooms();

        //방과 방 사이를 복도로 연결한다
        ConnectRooms();

        //바닥 주변 타일에 벽을 자동 배치한다
        CreateWalls();

        //실제 유니티 상에서 큐브로 타일을 그린다
        Render();

        //방 타입에 따라 적/보물을 배치한다.
        SpawnObjects();
    }


    //시작 방 1개 생성, 나머지는 기존 방 근처(상/하/좌/우)에 오프셋을 두고 시도
    //마지막 생성 방은 보스방으로 지정, 일반 방 일부를 보물방으로 변환
    void CreateRooms()
    {
        //시작 방 : 기준점(0.0)에 배치
        Vector2Int pos = Vector2Int.zero;
        int size = Random.Range(minSize, maxSize);
        AddRoom(pos, size, RoomType.Start);

        //나머지 방 생성 시도
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
        //1. 겹침 검사
        for(int x = -size / 2; x < size / 2; x++)
        {
            for(int y = -size / 2; y < size / 2; y++)
            {
                Vector2Int tile = center + new Vector2Int(x, y);
                if (floors.Contains(tile))
                    return false;
            }
        }
        //2. 방 메타데이터 등록
        Room room = new Room(center, size, type);
        rooms[center] = room;

        //3. 방 영역을 floors에 채운다.
        for (int x = -size / 2; x < size / 2; x++)
        {
            for (int y = -size / 2; y < size / 2; y++)
            {
                floors.Add(center + new Vector2Int(x, y));
            }
        }
        return true;

    }

    //모든 방을 직선 복도로 연결
    void ConnectRooms()
    {
        var roomList = new List<Room>(rooms.Values);

        for(int i = 0; i < roomList.Count - 1; i++)
        {
            CreateCorridor(roomList[i].centor, roomList[i + 1].centor);
        }
    }

    //두 좌표 사이를 x축 -> y축 순서로 직선 복도로 판다
    //굽이 치는 L자 모양이 나온다

    void CreateCorridor(Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;

        //x축 정렬 : start.x -> end.x로 한칸씩 이동하며 바닥 타일 추가
        while (current.x != end.x)
        {
            floors.Add(current);
            current.x += (end.x > current.x) ? 1 : -1;
        }
        //y축 정렬 :  x가 같아진 뒤 start.y -> end.y로 한칸씩 이동
        while (current.y != end.y)
        {
            floors.Add(current);
            current.y += (end.y > current.y) ? 1 : -1;
        }

        floors.Add(end);  //마지막 목적지도 바닥처리

    }

    //바닥 주변의 8방향을 스캔하여, 바닥이 아닌 칸을 walls로 채운다
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
    //타일을 유니티 오브젝트로 렌더링
    //바닥 : cube (0.1), 벽 cube (1), 방  색 구분
    void Render()
    {
        //바닥 타일 렌더링
        foreach(var pos in floors)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = new Vector3(pos.x, 0, pos.y);  //y = 0 평면에 배치
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

        //벽 타일 렌더링
        foreach(var pos in walls)
        {
          GameObject cube = GameObject.CreatePrimitive (PrimitiveType.Cube);
            cube.transform.position = new Vector3(pos.x, 0.5f, pos.y);
            cube.transform.SetParent (transform);
            cube.GetComponent<Renderer>().material.color = Color.black;
        }
    }
    //어떤 바닥 좌표가 "어느 방"에 속하는지 역추적
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
