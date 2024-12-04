using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SocketClient
{
    private const string Host = "127.0.0.1"; // サーバーのアドレス
    private const int Port = 8765;           // サーバーのポート番号
    private TcpClient client;                // クライアント用ソケット
    private NetworkStream stream;            // データ送受信用のストリーム
    private bool isRunning = true;           // クライアントの状態

    public void StartClient()
    {
        try
        {
            // サーバーに接続
            client = new TcpClient(Host, Port);
            stream = client.GetStream();
            isRunning = true;
            Debug.Log("サーバーに接続しました");


            // メッセージを受信するスレッドを開始
            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log($"サーバーへの接続に失敗しました: {e.Message}");
        }
    }

    private void ReceiveMessages()
    {
        try
        {
            while (isRunning)
            {
                if (stream == null || !client.Connected) break;

                // データを受信
                byte[] data = new byte[1024];
                int bytes = stream.Read(data, 0, data.Length);
                if (bytes == 0)
                {
                    Debug.Log("サーバーが切断しました");
                    break;
                }

                // メッセージを文字列に変換して表示
                string message = Encoding.UTF8.GetString(data, 0, bytes);
                Debug.Log($"サーバーからのメッセージ: {message}");
            }
        }
        catch (Exception e)
        {
            if (isRunning)
            {
                Debug.Log($"受信中にエラーが発生しました: {e.Message}");
            }
        }
        finally
        {
            CloseClient();
        }
    }

    public void CloseClient()
    {
        if (!isRunning) return;
        isRunning = false;

        try
        {
            stream?.Close();
            client?.Close();
            Debug.Log("クライアントを終了しました");
        }
        catch (Exception e)
        {
            Debug.Log($"終了中にエラーが発生しました: {e.Message}");
        }
    }
}
