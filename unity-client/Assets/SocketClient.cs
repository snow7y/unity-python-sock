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

    // メインスレッドでのみ取得可能なため、ここで格納する
    private string persistentDataPath;

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    // ファイル受信データをキューで受け渡すためのクラス
    private class ReceivedFileData
    {
        public string fileName;
        public long fileSize;
        public byte[] fileData;
    }
    private ConcurrentQueue<ReceivedFileData> fileQueue = new ConcurrentQueue<ReceivedFileData>();

    void Awake()
    {
        // メインスレッドでのみ呼び出せるためここでパスを確保
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
            Debug.Log("サーバーへの接続を試みます...");
            client = new TcpClient();
            client.Connect(serverIP, serverPort);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("サーバーに接続しました");

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError("サーバー接続中にエラーが発生しました: " + e.Message);
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
                Debug.Log("サーバーへメッセージを送信しました: " + message);
            }
            catch (Exception e)
            {
                Debug.LogError("メッセージ送信中にエラーが発生しました: " + e.Message);
            }
        }
        else
        {
            Debug.LogWarning("サーバーへ接続されていないためメッセージを送信できません");
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
                    Debug.Log("サーバーから切断されました");
                    isConnected = false;
                    shouldQuitApplication = true;
                    break;
                }

                string received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                lineBuffer.Append(received);

                // 改行で行区切り
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
                                // ファイル本体を受信
                                ReceiveFileData(fileName, fileSize);
                            }
                            else
                            {
                                Debug.LogError("ファイルサイズが不正: " + parts[2]);
                            }
                        }
                        else
                        {
                            Debug.LogError("FILEヘッダ形式が不正: " + line);
                        }
                    }
                    else
                    {
                        Debug.Log("不明な行メッセージ: " + line);
                    }
                }
            }
        }
        catch (Exception e)
        {
            // ここではUnityのメインスレッドAPIは呼べないため、ログ出力のみ
            Debug.LogError("受信中にエラーが発生しました: " + e.Message);
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
                Debug.LogError("ファイルデータ受信中にサーバーから切断されました");
                Disconnect();
                shouldQuitApplication = true;
                return;
            }
            Array.Copy(buf, 0, fileBuffer, totalRead, bytesRead);
            totalRead += bytesRead;
        }

        // ファイル受信完了データをキューに格納
        fileQueue.Enqueue(new ReceivedFileData { fileName = fileName, fileSize = fileSize, fileData = fileBuffer });
    }

    private void HandleCommand(string cmd)
    {
        // メインスレッドで処理するためQueueにいれる
        messageQueue.Enqueue("COMMAND:" + cmd);
    }

    void Update()
    {
        // ここはメインスレッド、UnityAPI使用可能
        // サーバーとの切断があればアプリケーションを終了
        if (shouldQuitApplication)
        {
            Debug.Log("アプリケーションを終了します");
            ApplicationQuit();
            shouldQuitApplication = false;
        }
        // コマンド処理
        while (messageQueue.TryDequeue(out string msg))
        {
            if (msg.StartsWith("COMMAND:"))
            {
                string cmd = msg.Substring("COMMAND:".Length);
                Debug.Log("メインスレッドでコマンド処理: " + cmd);
                // ここでコマンドに応じたUnity操作を行う
                string result = "";
                try
                {
                    if (cmd == "next")
                    {
                        Debug.Log("次のシーンに切り替える処理");
                        result = $"RESULT:Command '{cmd}' executed successfully.\n";
                    }
                    else if (cmd == "prev")
                    {
                        Debug.Log("前のシーンに切り替える処理");
                        result = $"RESULT:Command '{cmd}' executed successfully.\n";
                    }
                    else
                    {
                        Debug.LogWarning("不明なコマンド: " + cmd);
                        result = $"RESULT:Unknown command '{cmd}'.\n";
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("コマンド処理中にエラーが発生しました: " + e.Message);
                    result = $"RESULT:Error occurred while executing command '{cmd}': {e.Message}\n";
                }
                finally
                {
                    // 結果をサーバーに返信
                    SendMessageToServer(result);
                }
            }
        }

        // ファイル処理
        while (fileQueue.TryDequeue(out ReceivedFileData fdata))
        {
            // メインスレッドでのみpersistentDataPathを使用
            string savePath = Path.Combine(persistentDataPath, fdata.fileName);
            string result = "";
            try
            {
                File.WriteAllBytes(savePath, fdata.fileData);
                Debug.Log($"ファイル受信・保存完了: {savePath} (サイズ: {fdata.fileSize}バイト)");
                // ここで受信したファイルをUnityオブジェクトに適用する処理などを行う
                // ファイル受信完了をサーバーに通知
                result = $"RESULT:File '{fdata.fileName}' processed successfully.\n";
            }
            catch (Exception e)
            {
                Debug.LogError("ファイル保存中にエラー: " + e.Message);
                result = $"RESULT:File '{fdata.fileName}' processing failed: {e.Message}\n";
            }
            finally
            {
                // ファイル受信完了をサーバーに通知
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
            Debug.Log("サーバーとの接続を終了しました");
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
            // isConnected = falseでループ終了を誘発
            // ここでreceiveThread.Join(適宜タイムアウト)して確実にスレッドを終了させる
            receiveThread.Join();
        }
    }
}
