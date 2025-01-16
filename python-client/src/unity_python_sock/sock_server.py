import logging
import os
import socket

from unity_python_sock.commands import CommandBase

logger = logging.getLogger(__name__)


class SocketServer:
    """
    Socketサーバーを管理するクラス
    """

    def __init__(self, host="0.0.0.0", port=8765):
        self.host = host
        self.port = port
        self.server_socket = None
        self.client_socket = None
        self.client_address = None
        self.running = False
        self.is_connected = False

    def start(self) -> None:
        """
        サーバを起動
        """
        try:
            self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.server_socket.bind((self.host, self.port))
            self.server_socket.listen(1)
            logger.info(f"サーバーが起動しました: {self.host}:{self.port}")
            self.running = True
        except OSError as e:
            logger.error(f"サーバーの起動中にエラーが発生しました: {e}")
            self.stop()
        except Exception as e:
            logger.error(f"サーバーの起動中にエラーが発生しました: {e}")
            self.stop()

        try:
            while self.running:
                logger.info("クライアントの接続を待機中...")
                self.client_socket, self.client_address = self.server_socket.accept()
                logger.info(f"クライアントが接続しました: {self.client_address}")
                self.handle_client(self.client_socket)
        except KeyboardInterrupt:
            logger.info("サーバーを停止します")
        except Exception as e:
            logger.error(f"サーバーでエラーが発生しました {e}")
        finally:
            self.stop()

    def stop(self) -> None:
        """
        サーバーを停止
        """
        self.running = False
        self.is_connected = False
        if self.client_socket:
            self.client_socket.close()
        if self.server_socket:
            self.server_socket.close()
        logger.info("サーバーを完全に停止しました")

    def _wait_for_result(self) -> str:
        """
        クライアントからRESPONSEプロトコルを受け取る
        """
        buffer = ""
        print("Waiting for result")
        while True:
            print("test")
            data = self.client_socket.recv(1024).decode("utf-8")
            print(f"Data: {data}")
            if not data:
                raise ConnectionError("クライアントとの接続が切断されました")
            buffer += data
            print(f"Buffer: {buffer}")

            # ヘッダー処理
            newline_index = buffer.find("\n")
            if newline_index == -1:
                continue  # 改行が見つからない場合は次のデータを待つ

            header = buffer[:newline_index].strip()
            buffer = buffer[newline_index + 1:]  # ヘッダー部分を削除

            if not header.startswith("RESPONSE"):
                logger.error(f"不明なヘッダー形式: {header}")
                continue

            # ヘッダー解析
            parts = header.split(" ")
            if len(parts) != 2 or not parts[1].isdigit():
                logger.error(f"ヘッダー形式が不正: {header}")
                continue

            body_size = int(parts[1])

            # ボディ受信
            while len(buffer) < body_size:
                data = self.client_socket.recv(1024).decode("utf-8")
                if not data:
                    raise ConnectionError("クライアントとの接続が切断されました")
                buffer += data

            body = buffer[:body_size]
            buffer = buffer[body_size:]  # ボディ部分を削除

            logger.info(f"受信したレスポンス: ヘッダー={header}, ボディ={body}")
            return body


    def handle_client(self, client_socket: socket.socket) -> None:
        """
        クライアントとの通信を処理するスレッドを管理

        Args:
            client_socket (socket.socket): クライアントとの通信用ソケット
        """
        self.is_connected = True
        try:
            # クライアントからのデータを受け取る処理（必要なら実装）
            while self.running:
                data = client_socket.recv(1024).decode("utf-8")
                if not data:
                    break
                logger.debug(f"クライアントからのデータ: {data}")
        except Exception as e:
            logger.error(f"クライアント処理中にエラーが発生しました: {e}")
        finally:
            client_socket.close()
            self.is_connected = False
            logger.info("クライアントとの接続を終了しました")

    def send_command(self, command: CommandBase) -> str:
        """
        クライアントにコマンドを送信

        Args:
            command (CommandBase): 送信するコマンドのインスタンス
        """
        if self.client_socket:
            try:
                command.convert_body()
                full_message = command.get_command()
                self.client_socket.sendall(full_message.encode("utf-8"))

                # コマンドの実行結果を待機
                result = self._wait_for_result()
                logger.info(f"コマンドの実行結果: {result}")
                return result
            except Exception as e:
                logger.error(f"コマンド送信中にエラーが発生しました: {e}")
        else:
            logger.warning("クライアントが接続されていません")

    def send_file(self, file_path: str) -> str:
        """
        クライアントにファイルを送信

        Args:
            file_path (str): 送信するファイルのパス

        Raises:
            FileNotFoundError: ファイルが見つからない場合
            e: その他のエラー
        """
        if self.client_socket:
            try:
                if not os.path.isfile(file_path):
                    logger.error(f"ファイルが見つかりません: {file_path}")
                    raise FileNotFoundError(f"ファイルが見つかりません: {file_path}")

                # ファイル情報を送信
                file_name = os.path.basename(file_path)
                file_size = os.path.getsize(file_path)
                file_header = f"FILE:{file_name}:{file_size}\n"
                self.client_socket.sendall(file_header.encode("utf-8"))

                # ファイル内容を送信
                with open(file_path, "rb") as f:
                    while chunk := f.read(1024):
                        self.client_socket.sendall(chunk)

                logger.debug(f"ファイルを送信しました: {file_path}")

                # ファイルの送信結果を待機
                result = self._wait_for_result()
                logger.info(f"ファイルの送信結果: {result}")
                return result
            except Exception as e:
                raise e
        else:
            logger.warning("クライアントが接続されていません")
