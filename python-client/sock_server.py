import socket
import threading

# サーバーの設定
HOST = '127.0.0.1'  # ローカルホスト
PORT = 8765         # 使用するポート番号

# 接続されたクライアントを保持する変数
connected_client = None
connected_address = None

# クライアントハンドラ（接続されたクライアントを処理）
def handle_client(client_socket, address):
    global connected_client, connected_address
    print(f"クライアントが接続しました: {address}")
    connected_client = client_socket
    connected_address = address
    try:
        while True:
            # クライアントからのデータを受信
            message = client_socket.recv(1024).decode('utf-8')
            if not message:
                print(f"クライアントが切断しました: {address}")
                break
            print(f"クライアントから受信: {message}")
    except Exception as e:
        print(f"エラーが発生しました: {e}")
    finally:
        connected_client = None
        connected_address = None
        client_socket.close()

# サーバーを開始する関数
def start_server():
    global connected_client
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.bind((HOST, PORT))
    server.listen(1)  # 接続を1クライアントに限定
    print(f"サーバーが起動しました: {HOST}:{PORT}")
    try:
        while True:
            if connected_client is None:
                print("クライアントの接続待機中...")
                client_socket, address = server.accept()
                client_thread = threading.Thread(
                    target=handle_client, args=(client_socket, address))
                client_thread.start()
    except KeyboardInterrupt:
        print("サーバーを停止します")
    finally:
        server.close()

# 任意のタイミングでメッセージを送信する関数
def send_message_to_client():
    global connected_client, connected_address
    while True:
        if connected_client:
            message = input("クライアントに送信するメッセージを入力してください (終了するには'q'): ")
            if message.lower() == 'q':
                print("メッセージ送信を終了します")
                break
            try:
                connected_client.send(message.encode('utf-8'))
                print(f"送信済み: {message}")
            except Exception as e:
                print(f"送信中にエラーが発生しました: {e}")


if __name__ == "__main__":
    # サーバーを別スレッドで起動
    server_thread = threading.Thread(target=start_server)
    server_thread.start()

    # メッセージ送信の制御をメインスレッドで実行
    send_message_to_client()
