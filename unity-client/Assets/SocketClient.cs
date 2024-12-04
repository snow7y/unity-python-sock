using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class SocketClient
{
    private const string Host = "127.0.0.1"; // �T�[�o�[�̃A�h���X
    private const int Port = 8765;           // �T�[�o�[�̃|�[�g�ԍ�
    private TcpClient client;                // �N���C�A���g�p�\�P�b�g
    private NetworkStream stream;            // �f�[�^����M�p�̃X�g���[��
    private bool isRunning = true;           // �N���C�A���g�̏��

    public void StartClient()
    {
        try
        {
            // �T�[�o�[�ɐڑ�
            client = new TcpClient(Host, Port);
            stream = client.GetStream();
            isRunning = true;
            Debug.Log("�T�[�o�[�ɐڑ����܂���");


            // ���b�Z�[�W����M����X���b�h���J�n
            Thread receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log($"�T�[�o�[�ւ̐ڑ��Ɏ��s���܂���: {e.Message}");
        }
    }

    private void ReceiveMessages()
    {
        try
        {
            while (isRunning)
            {
                if (stream == null || !client.Connected) break;

                // �f�[�^����M
                byte[] data = new byte[1024];
                int bytes = stream.Read(data, 0, data.Length);
                if (bytes == 0)
                {
                    Debug.Log("�T�[�o�[���ؒf���܂���");
                    break;
                }

                // ���b�Z�[�W�𕶎���ɕϊ����ĕ\��
                string message = Encoding.UTF8.GetString(data, 0, bytes);
                Debug.Log($"�T�[�o�[����̃��b�Z�[�W: {message}");
            }
        }
        catch (Exception e)
        {
            if (isRunning)
            {
                Debug.Log($"��M���ɃG���[���������܂���: {e.Message}");
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
            Debug.Log("�N���C�A���g���I�����܂���");
        }
        catch (Exception e)
        {
            Debug.Log($"�I�����ɃG���[���������܂���: {e.Message}");
        }
    }
}
