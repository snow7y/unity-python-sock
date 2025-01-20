using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;

public class SocketClient : MonoBehaviour
{
    public string serverIP = "127.0.0.1";
    public int serverPort = 8765;

    private TcpClient client;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;

    private string persistentDataPath;

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    private class ReceivedFileData
    {
        public string fileName;
        public long fileSize;
        public byte[] fileData;
    }
    private ConcurrentQueue<ReceivedFileData> fileQueue = new ConcurrentQueue<ReceivedFileData>();

    void Awake()
    {
        persistentDataPath = Application.persistentDataPath;
    }

    void Start()
    {
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            Debug.Log("�T�[�o�[�ւ̐ڑ������݂܂�...");
            client = new TcpClient();
            client.Connect(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("�T�[�o�[�ɐڑ����܂���");

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("�T�[�o�[�ڑ����ɃG���[���������܂���: " + e.Message);
            isConnected = false;
        }
    }

    public void SendMessageToServer(string message)
    {
        if (isConnected && stream != null)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
                Debug.Log("�T�[�o�[�փ��b�Z�[�W�𑗐M���܂���: " + message);
            }
            catch (Exception e)
            {
                Debug.LogError("���b�Z�[�W���M���ɃG���[���������܂���: " + e.Message);
            }
        }
        else
        {
            Debug.LogWarning("�T�[�o�[�֐ڑ�����Ă��Ȃ����߃��b�Z�[�W�𑗐M�ł��܂���");
        }
    }

    private void ReceiveData()
    {
        try
        {
            byte[] buffer = new byte[1024];
            StringBuilder messageBuffer = new StringBuilder();

            while (isConnected)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) throw new Exception("�T�[�o�[�Ƃ̐ڑ����ؒf����܂���");

                messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                while (true)
                {
                    string message = messageBuffer.ToString();
                    int newlineIndex = message.IndexOf('\n');
                    if (newlineIndex < 0) break;

                    string header = message.Substring(0, newlineIndex).Trim();
                    messageBuffer.Remove(0, newlineIndex + 1);

                    Debug.Log($"��M�����w�b�_�[: {header}");

                    string[] parts = header.Split(' ');
                    if (parts.Length != 2)
                    {
                        Debug.LogError("�w�b�_�[�`�����s��");
                        break;
                    }

                    string commandType = parts[0];
                    int bodySize = int.Parse(parts[1]);

                    if (messageBuffer.Length < bodySize) break;

                    string body = messageBuffer.ToString(0, bodySize);
                    messageBuffer.Remove(0, bodySize);

                    Debug.Log($"��M����BODY: {body}");

                    HandleCommand(commandType, body);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"��M�������ɃG���[������: {e.Message}");
            Disconnect();
        }
    }

    private void HandleCommand(string commandType, string body)
    {
        Debug.Log($"HandleCommand�Ăяo�� - CommandType: {commandType}, Body: {body}");
        switch (commandType)
        {
            case "CONTROL":
                HandleControlCommand(body);
                break;
            case "SCENE":
                HandleSceneCommand(body);
                break;
            case "UPDATE":
                HandleUpdateCommand(body);
                break;
            case "NEXT":
                SendResponse("{\"status_code\": 200, \"status_message\": \"OK\", \"result\": \"next command sccess\"}");
                break;
            case "PREVIOUS":
                SendResponse("{\"status_code\": 200, \"status_message\": \"OK\", \"result\": \"previous command sccess\"}");
                break;
            case "TRANSFER":
                HandleTransferCommand(body);
                break;
            case "PING":
                SendResponse("{\"status_code\": 200, \"status_message\": \"OK\", \"result\": \"ping command sccess\"}");
                break;
            default:
                Debug.LogWarning($"�s���ȃR�}���h�^�C�v: {commandType}");
                SendResponse("{\"status_code\": 400, \"status_message\": \"Bad Request\", \"result\": \"Unknown command\"}");
                break;
        }
    }

    private void HandleControlCommand(string body)
    {
        try
        {
            var commandData = JsonUtility.FromJson<ControlCommandData>(body);
            Debug.Log($"�I�u�W�F�N�g {commandData.object_id} �ɑ΂��鑀��: {commandData.action}");
            // �����ŃI�u�W�F�N�g�̑�������s����(��������2�b�X���[�v)
            Thread.Sleep(2000);
            SendResponse("{\"status_code\": 200, \"status_message\": \"OK\", \"result\": \"success\"}");
        }
        catch (Exception e)
        {
            Debug.LogError($"CONTROL�R�}���h�������ɃG���[: {e.Message}");
            SendResponse("{\"status_code\": 500, \"status_message\": \"Internal Server Error\", \"result\": \"Failed to process CONTROL command\"}");
        }
    }

    private void HandleSceneCommand(string body)
    {
        try
        {
            var sceneData = JsonUtility.FromJson<SceneCommandData>(body);
            Debug.Log($"�V�[���� {sceneData.scene_name} �ɐ؂�ւ��܂�");
            SendResponse("{\"status_code\": 200, \"status_message\": \"OK\", \"result\": \"scene changed\"}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SCENE�R�}���h�������ɃG���[: {e.Message}");
            SendResponse("{\"status_code\": 500, \"status_message\": \"Internal Server Error\", \"result\": \"Failed to change scene\"}");
        }
    }

    private void HandleUpdateCommand(string body)
    {
        try
        {
            var updateData = JsonUtility.FromJson<UpdateCommandData>(body);
            Debug.Log($"�I�u�W�F�N�g {updateData.object_id} �� {updateData.file_name} �ɍX�V���܂�");
            SendResponse("{\"status_code\": 200, \"status_message\": \"OK\", \"result\": \"object updated\"}");
        }
        catch (Exception e)
        {
            Debug.LogError($"UPDATE�R�}���h�������ɃG���[: {e.Message}");
            SendResponse("{\"status_code\": 500, \"status_message\": \"Internal Server Error\", \"result\": \"Failed to update object\"}");
        }
    }

    private void HandleTransferCommand(string body)
    {
        try
        {
            var transferData = JsonUtility.FromJson<TransferCommandData>(body);
            SendResponse("{\"status_code\": 200, \"status_message\": \"OK\", \"result\": \"file receive to ready\"}");
            ReceiveFileData(transferData.file_name, transferData.file_size);
        }
        catch (Exception e)
        {
            Debug.LogError($"TRANSFER�R�}���h�������ɃG���[: {e.Message}");
            SendResponse("{\"status_code\": 500, \"status_message\": \"Internal Server Error\", \"result\": \"Failed to process TRANSFER command\"}");
        }
    }

    private void ReceiveFileData(string fileName, long fileSize)
    {
        try
        {
            byte[] fileBuffer = new byte[fileSize];
            int totalRead = 0;

            while (totalRead < fileSize)
            {
                int bytesRead = stream.Read(fileBuffer, totalRead, fileBuffer.Length - totalRead);
                if (bytesRead == 0)
                {
                    throw new Exception("�T�[�o�[����ؒf����܂���");
                }
                totalRead += bytesRead;
            }

            string savePath = Path.Combine(persistentDataPath, fileName);
            File.WriteAllBytes(savePath, fileBuffer);
            Debug.Log($"�t�@�C����M�E�ۑ�����: {savePath} (�T�C�Y: {fileSize}�o�C�g)");

            SendResponse("{\"status_code\": 200, \"status_message\": \"OK\", \"result\": \"file received\"}");
        }
        catch (Exception e)
        {
            Debug.LogError($"�t�@�C����M�������ɃG���[: {e.Message}");
            SendResponse("{\"status_code\": 500, \"status_message\": \"Internal Server Error\", \"result\": \"Failed to receive file\"}");
        }
    }

    private void SendResponse(string responseBody)
    {
        responseBody += "\n";

        int bodySize = Encoding.UTF8.GetBytes(responseBody).Length;
        string header = $"RESPONSE {bodySize}\n";
        string message = header + responseBody;
        byte[] data = Encoding.UTF8.GetBytes(message);
        //byte[] headerBytes = Encoding.UTF8.GetBytes(header);
        //byte[] bodyBytes = Encoding.UTF8.GetBytes(responseBody);
        //byte[] message = new byte[headerBytes.Length + bodyBytes.Length];
        //headerBytes.CopyTo(message, 0);
        //bodyBytes.CopyTo(message, headerBytes.Length);

        try
        {
            stream.Write(data, 0, data.Length);
            Debug.Log($"���X�|���X���M: {message}");
            //Debug.Log($"���X�|���X���M: {header.Trim()} {responseBody}");
        }
        catch (Exception e)
        {
            Debug.LogError($"���X�|���X���M���ɃG���[���������܂���: {e.Message}");
        }
    }

    private void Disconnect()
    {
        if (isConnected)
        {
            isConnected = false;
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
            if (client != null)
            {
                client.Close();
                client = null;
            }
            Debug.Log("�T�[�o�[�Ƃ̐ڑ����I�����܂���");
        }
    }

    [Serializable]
    private class ControlCommandData
    {
        public string object_id;
        public string action;
    }

    [Serializable]
    private class SceneCommandData
    {
        public string scene_name;
    }

    [Serializable]
    private class UpdateCommandData
    {
        public string object_id;
        public string file_name;
    }

    [Serializable]
    private class TransferCommandData
    {
        public string file_name;
        public long file_size;
    }

    void OnApplicationQuit()
    {
        Disconnect();
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join();
        }
    }
}
