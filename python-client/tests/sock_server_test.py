import logging
from queue import Empty, Queue
from threading import Thread
from time import sleep

from unity_python_sock.commands import (
    ControlCommand,
    TransferCommand,
    ListCommand,
    NextCommand,
    PreviousCommand,
    PingCommand,
)
from unity_python_sock.sock_server import SocketServer

logging.basicConfig(level=logging.DEBUG)

server = SocketServer()
server_thread = Thread(target=server.start, daemon=True)
server_thread.start()

print("クライアントの接続を待機中...")


input_queue = Queue()


def input_thread(queue):
    """ユーザー入力を非同期にキューに追加するスレッド"""
    while True:
        user_input = input()  # ブロッキングな入力
        queue.put(user_input)  # 入力をキューに送る


Thread(target=input_thread, args=(input_queue,), daemon=True).start()


def get_input():
    while True:
        try:
            print("選択してください: ", end="", flush=True)
            option = input_queue.get(timeout=60)  # 60秒間待機
            if not isinstance(option, str):
                print("無効な入力です。もう一度試してください")
                continue
            return option
        except Empty:
            print("入力がタイムアウトしました。")
            break


# コマンドの種類を選ぶメニューを表示
def display_menu():
    print("\n\n送信オプション: ")
    print("----------------")
    print("1: controlコマンドを送信")
    print("2: transferコマンドを送信")
    print("3: nextコマンドを送信")
    print("4: previousコマンドを送信")
    print("5: listコマンドを送信")
    print("6: pingコマンドを送信")
    print("q: サーバーを停止")
    print("----------------")
    


def choose_command(command_num: str):
    match command_num:
        case "1":
            action = get_input()
            command_obj = ControlCommand(object_id="1", action=action, action_parameters={"param1": "value1"})
        case "2":
            file_path = get_input()
            command_obj = TransferCommand(file_path)
        case "3":
            command_obj = NextCommand()
        case "4":
            command_obj = PreviousCommand()
        case "5":
            command_obj = ListCommand()
        case "6":
            command_obj = PingCommand()
        case "q":
            server.stop()
            return
        case _:
            print("無効な選択です。もう一度試してください")
            return
        
    server.send_command(command_obj)


def main():
    last_connection_state = server.is_connected  # 接続状態を記録

    try:
        while True:
            # 接続状態が変化した場合のみメッセージを表示
            if server.is_connected != last_connection_state:
                last_connection_state = server.is_connected
                if not server.is_connected:
                    print("サーバーとの接続がありません。再接続を待機しています...")
                else:
                    print("サーバーに接続されました。")

            # サーバーが接続されていない場合は再接続を待機
            if not server.is_connected:
                sleep(1)
                continue

            display_menu()
            option = get_input()
            choose_command(option)
    except KeyboardInterrupt:
        print("\nサーバーを停止します")
        server.stop()


if __name__ == "__main__":
    main()


# try:
#     last_connection_state = server.is_connected  # 接続状態を記録
#     menu_displayed = False

#     while True:
#         # 接続状態が変化した場合のみメッセージを表示
#         if server.is_connected != last_connection_state:
#             last_connection_state = server.is_connected
#             menu_displayed = False
#             if not server.is_connected:
#                 print("サーバーとの接続がありません。再接続を待機しています...")
#             else:
#                 print("サーバーに接続されました。")

#         # サーバーが接続されていない場合は再接続を待機
#         if not server.is_connected:
#             sleep(1)
#             continue

#         # メニューを一度だけ表示
#         if not menu_displayed:
#             print("\n送信オプション: ")
#             print("1: コマンドを送信")
#             print("2: ファイルを送信")
#             print("q: サーバーを停止")
#             print("選択してください: ", end="", flush=True)
#             menu_displayed = True

#         # キューから入力を取得
#         try:
#             option = input_queue.get(timeout=1)  # 1秒間待機
#         except Empty:
#             continue

#         # 入力後に接続状態を確認
#         if not server.is_connected:
#             print("接続が失われました。再接続を待機してください。")
#             menu_displayed = False
#             continue

#         # 入力に応じた処理
#         if option == "1":
#             print("送信するコマンド: ", end="", flush=True)
#             try:
#                 command = input_queue.get(timeout=30)  # コマンド入力を待機
#             except Empty:
#                 print("コマンド入力がタイムアウトしました。")
#                 continue

#             if not server.is_connected:
#                 print("接続が失われました。コマンド送信をキャンセルしました。")
#                 continue
#             command_obj = ControlCommand(object_id="1", action=command, action_parameters={"param1": "value1"})
#             server.send_command(command_obj)
#             print("選択してください: ", end="", flush=True)
#         elif option == "2":
#             print("送信するファイルのパス: ", end="", flush=True)
#             try:
#                 file_path = input_queue.get(timeout=30)  # ファイルパス入力を待機
#             except Empty:
#                 print("ファイルパス入力がタイムアウトしました。")
#                 continue

#             if not server.is_connected:
#                 print("接続が失われました。ファイル送信をキャンセルしました。")
#                 continue
#             command_obj = TransferCommand(file_path)
#             server.send_file(command_obj)
#             print("選択してください: ", end="", flush=True)
#         elif option.lower() == "q":
#             server.stop()
#             break
#         else:
#             print("無効な選択です。もう一度試してください")
# except FileNotFoundError as e:
#     print(f"ファイルが見つかりません: {e}")
# except KeyboardInterrupt:
#     print("\nサーバーを停止します")
#     server.stop()
