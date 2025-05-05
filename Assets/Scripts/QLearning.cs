using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class QLearningAgent : MonoBehaviour
{
    public static QLearningAgent Instance;

    public GameObject basket;
    public GameObject applePrefab;
    public float spawnInterval = 2.5f;

    private float timer = 0f;
    private Dictionary<string, float[]> qTable = new Dictionary<string, float[]>();
    private Vector3[] actions = {
        new Vector3(-0.5f, 0, 0), 
        Vector3.zero,             
        new Vector3(0.5f, 0, 0)  
    };

    private GameObject targetApple;
    private float decisionTimer = 0f;
    private float decisionInterval = 0.1f;

    public float learningRate = 0.1f;
    public float discountFactor = 0.95f;
    public float explorationRate = 0.3f;
    public float explorationDecay = 0.99f;

    private int applesCaught = 0;
    private int applesMissed = 0;

    private string qTableFilePath = "Assets/Resources/qTable.json";  

    void Awake()
    {
        Instance = this;
        LoadQTable();  
    }

    void Start()
    {
        timer = spawnInterval;
    }

    void Update()
    {
        Time.timeScale = 2f; 

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnApple();
            timer = 0f;
        }

        if (targetApple == null)
        {
            FindClosestApple();
        }

        decisionTimer += Time.deltaTime;
        if (decisionTimer >= decisionInterval && targetApple != null)
        {
            DoQLearning();
            decisionTimer = 0f;
        }

        // Estatísticas
        if (Time.frameCount % 300 == 0)
        {
            float successRate = (applesCaught + applesMissed) > 0 ? (float)applesCaught / (applesCaught + applesMissed) : 0f;
            Debug.Log($"Taxa de sucesso: {(successRate * 100f):F2}% | Acertos: {applesCaught} | Erros: {applesMissed} | Q-table: {qTable.Count} | Exploração: {explorationRate:F2}");
        }
    }

    void SpawnApple()
    {
        if (applePrefab == null)
        {
            Debug.LogError("Apple prefab não está atribuído!");
            return;
        }

        Vector3 spawnPos = new Vector3(Random.Range(-7f, 7f), 6f, 0f);
        Instantiate(applePrefab, spawnPos, Quaternion.identity).tag = "Apple";
    }

    void FindClosestApple()
    {
        var apples = GameObject.FindGameObjectsWithTag("Apple");
        if (apples.Length == 0)
        {
            targetApple = null;
            return;
        }

        targetApple = apples[0];
        float closestDistance = Mathf.Abs(targetApple.transform.position.x - basket.transform.position.x);

        foreach (var apple in apples)
        {
            float distance = Mathf.Abs(apple.transform.position.x - basket.transform.position.x);
            if (distance < closestDistance)
            {
                targetApple = apple;
                closestDistance = distance;
            }
        }
    }

    void DoQLearning()
    {
        string state = GetState();
        if (!qTable.ContainsKey(state))
            qTable[state] = new float[3];

        int action = ChooseAction(state);
        Vector3 moveDirection = actions[action];
        basket.transform.Translate(moveDirection);

        Vector3 pos = basket.transform.position;
        pos.x = Mathf.Clamp(pos.x, -7.5f, 7.5f);
        basket.transform.position = pos;

        if (targetApple != null && targetApple.transform.position.y < -5f)
        {
            float reward = -10f;

            if (Mathf.Abs(basket.transform.position.x - targetApple.transform.position.x) < 1.5f)
            {
                reward = 20f;
                applesCaught++;
            }
            else
            {
                applesMissed++;
            }

            string newState = GetState();
            if (!qTable.ContainsKey(newState))
                qTable[newState] = new float[3];

            qTable[state][action] = (1 - learningRate) * qTable[state][action] +
                                    learningRate * (reward + discountFactor * MaxQ(newState));

            Destroy(targetApple);
            targetApple = null;

            explorationRate *= explorationDecay;
            explorationRate = Mathf.Max(0.01f, explorationRate);

            SaveQTable();  
        }
    }

    int ChooseAction(string state)
    {
        if (Random.value < explorationRate)
            return Random.Range(0, 3);

        float[] actionsQ = qTable[state];
        int bestAction = 0;
        float bestValue = actionsQ[0];

        for (int i = 1; i < actionsQ.Length; i++)
        {
            if (actionsQ[i] > bestValue)
            {
                bestValue = actionsQ[i];
                bestAction = i;
            }
        }
        return bestAction;
    }

    float MaxQ(string state)
    {
        float[] actionsQ = qTable[state];
        float max = actionsQ[0];

        for (int i = 1; i < actionsQ.Length; i++)
        {
            if (actionsQ[i] > max)
                max = actionsQ[i];
        }
        return max;
    }

    string GetState()
    {
        if (targetApple == null)
            return "none";

        int basketX = Discretize(basket.transform.position.x);
        int appleX = Discretize(targetApple.transform.position.x);

        return $"{basketX},{appleX}";
    }

    int Discretize(float x)
    {
        return Mathf.RoundToInt(x);
    }

    void LoadQTable()
    {
        if (File.Exists(qTableFilePath))
        {
            string json = File.ReadAllText(qTableFilePath);
            qTable = JsonUtility.FromJson<QTableWrapper>(json).qTable;
            Debug.Log("Q-table carregada com sucesso!");
        }
        else
        {
            Debug.LogWarning("Arquivo da Q-table não encontrado. Iniciando com uma Q-table vazia.");
        }
    }

    public void OnAppleCaught()
    {
        applesCaught++; 
        Debug.Log($"Maçã capturada! Total: {applesCaught}");
    }


    void SaveQTable()
    {
        string directoryPath = Path.GetDirectoryName(qTableFilePath);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        QTableWrapper wrapper = new QTableWrapper { qTable = qTable };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(qTableFilePath, json);
    }

}
