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
    private bool shouldQuitApplication = false;

    // ���C���X���b�h�ł̂ݎ擾�\�Ȃ��߁A�����Ŋi�[����
    private string persistentDataPath;

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    // �t�@�C����M�f�[�^���L���[�Ŏ󂯓n�����߂̃N���X
    private class ReceivedFileData
    {
        public string fileName;
        public long fileSize;
        public byte[] fileData;
    }
    private ConcurrentQueue<ReceivedFileData> fileQueue = new ConcurrentQueue<ReceivedFileData>();

    void Awake()
    {
        // ���C���X���b�h�ł̂݌Ăяo���邽�߂����Ńp�X���m��
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
            shouldQuitApplication = true;
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
            StringBuilder lineBuffer = new StringBuilder();

            while (isConnected)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Debug.Log("�T�[�o�[����ؒf����܂���");
                    isConnected = false;
                    shouldQuitApplication = true;
                    break;
                }

                string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                lineBuffer.Append(received);

                // ���s�ōs��؂�
                while (true)
                {
                    string fullText = lineBuffer.ToString();
                    int newlineIndex = fullText.IndexOf('\n');
                    if (newlineIndex < 0)
                        break;

                    string line = fullText.Substring(0, newlineIndex).Trim('\r', '\n');
                    lineBuffer.Remove(0, newlineIndex + 1);

                    if (line.StartsWith("COMMAND:"))
                    {
                        string cmd = line.Substring("COMMAND:".Length);
                        HandleCommand(cmd);
                    }
                    else if (line.StartsWith("FILE:"))
                    {
                        string[] parts = line.Split(':');
                        if (parts.Length == 3)
                        {
                            string fileName = parts[1];
                            if (long.TryParse(parts[2], out long fileSize))
                            {
                                // �t�@�C���{�̂���M
                                ReceiveFileData(fileName, fileSize);
                            }
                            else
                            {
                                Debug.LogError("�t�@�C���T�C�Y���s��: " + parts[2]);
                            }
                        }
                        else
                        {
                            Debug.LogError("FILE�w�b�_�`�����s��: " + line);
                        }
                    }
                    else
                    {
                        Debug.Log("�s���ȍs���b�Z�[�W: " + line);
                    }
                }
            }
        }
        catch (Exception e)
        {
            // �����ł�Unity�̃��C���X���b�hAPI�͌ĂׂȂ����߁A���O�o�͂̂�
            Debug.LogError("��M���ɃG���[���������܂���: " + e.Message);
            Disconnect();
        }
    }

    private void ReceiveFileData(string fileName, long fileSize)
    {
        byte[] fileBuffer = new byte[fileSize];
        int totalRead = 0;
        while (totalRead < fileSize)
        {
            int remain = (int)(fileSize - totalRead);
            int readSize = Math.Min(remain, 1024);
            byte[] buf = new byte[readSize];
            int bytesRead = stream.Read(buf, 0, readSize);
            if (bytesRead == 0)
            {
                Debug.LogError("�t�@�C���f�[�^��M���ɃT�[�o�[����ؒf����܂���");
                Disconnect();
                shouldQuitApplication = true;
                return;
            }
            Array.Copy(buf, 0, fileBuffer, totalRead, bytesRead);
            totalRead += bytesRead;
        }

        // �t�@�C����M�����f�[�^���L���[�Ɋi�[
        fileQueue.Enqueue(new ReceivedFileData { fileName = fileName, fileSize = fileSize, fileData = fileBuffer });
    }

    private void HandleCommand(string cmd)
    {
        // ���C���X���b�h�ŏ������邽��Queue�ɂ����
        messageQueue.Enqueue("COMMAND:" + cmd);
    }

    void Update()
    {
        // �����̓��C���X���b�h�AUnityAPI�g�p�\
        // �T�[�o�[�Ƃ̐ؒf������΃A�v���P�[�V�������I��
        if (shouldQuitApplication)
        {
            Debug.Log("�A�v���P�[�V�������I�����܂�");
            ApplicationQuit();
            shouldQuitApplication = false;
        }
        // �R�}���h����
        while (messageQueue.TryDequeue(out string msg))
        {
            if (msg.StartsWith("COMMAND:"))
            {
                string cmd = msg.Substring("COMMAND:".Length);
                Debug.Log("���C���X���b�h�ŃR�}���h����: " + cmd);
                // �����ŃR�}���h�ɉ�����Unity������s��
                string result = "";
                try
                {
                    if (cmd == "next")
                    {
                        Debug.Log("���̃V�[���ɐ؂�ւ��鏈��");
                        result = $"RESULT:Command '{cmd}' executed successfully.\n";
                    }
                    else if (cmd == "prev")
                    {
                        Debug.Log("�O�̃V�[���ɐ؂�ւ��鏈��");
                        result = $"RESULT:Command '{cmd}' executed successfully.\n";
                    }
                    else
                    {
                        Debug.LogWarning("�s���ȃR�}���h: " + cmd);
                        result = $"RESULT:Unknown command '{cmd}'.\n";
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("�R�}���h�������ɃG���[���������܂���: " + e.Message);
                    result = $"RESULT:Error occurred while executing command '{cmd}': {e.Message}\n";
                }
                finally
                {
                    // ���ʂ��T�[�o�[�ɕԐM
                    SendMessageToServer(result);
                }
            }
        }

        // �t�@�C������
        while (fileQueue.TryDequeue(out ReceivedFileData fdata))
        {
            // ���C���X���b�h�ł̂�persistentDataPath���g�p
            string savePath = Path.Combine(persistentDataPath, fdata.fileName);
            string result = "";
            try
            {
                File.WriteAllBytes(savePath, fdata.fileData);
                Debug.Log($"�t�@�C����M�E�ۑ�����: {savePath} (�T�C�Y: {fdata.fileSize}�o�C�g)");
                // �����Ŏ�M�����t�@�C����Unity�I�u�W�F�N�g�ɓK�p���鏈���Ȃǂ��s��
                // �t�@�C����M�������T�[�o�[�ɒʒm
                result = $"RESULT:File '{fdata.fileName}' processed successfully.\n";
            }
            catch (Exception e)
            {
                Debug.LogError("�t�@�C���ۑ����ɃG���[: " + e.Message);
                result = $"RESULT:File '{fdata.fileName}' processing failed: {e.Message}\n";
            }
            finally
            {
                // �t�@�C����M�������T�[�o�[�ɒʒm
                SendMessageToServer(result);
            }
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

    private void ApplicationQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void OnApplicationQuit()
    {
        Disconnect();
        if (receiveThread != null && receiveThread.IsAlive)
        {
            // isConnected = false�Ń��[�v�I����U��
            // ������receiveThread.Join(�K�X�^�C���A�E�g)���Ċm���ɃX���b�h���I��������
            receiveThread.Join();
        }
    }
}
