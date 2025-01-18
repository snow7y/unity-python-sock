import json
import logging

logger = logging.getLogger(__name__)


class CommandBase:
    """
    コマンドの基底クラス
    """

    def __init__(self, command_name: str | None = None, body_size: int | None = None, command_body: dict | None = None):
        self._command_name = command_name
        self._body_size = body_size
        self._command_body = command_body

    def __str__(self):
        try:
            return (
                f"command_name: {self.command_name}, \nbody_size: {self.body_size}, \ncommand_body: {self.command_body}"
            )
        except ValueError as e:
            logger.warning(f"コマンドが不正です: {e}")
            return (
                f"command_name: {self._command_name}, \n"
                f"body_size: {self._body_size}, \n"
                f"command_body: {self._command_body}"
            )

    def get_command(self):
        return f"{self.command_header}\n{self.get_body_to_str()}"

    @property
    def command_name(self) -> str:
        if self._command_name is None:
            raise ValueError("command_nameが設定されていません")
        return self._command_name

    @command_name.setter
    def command_name(self, value: str):
        if not isinstance(value, str):
            raise ValueError("command_nameは文字列で指定してください")
        self._command_name = value

    @property
    def body_size(self) -> int:
        if self._body_size is None:
            raise ValueError("body_sizeが設定されていません。command_bodyを設定してください")
        return self._body_size

    @property
    def command_body(self) -> dict:
        if self._command_body is None:
            raise ValueError("command_bodyが設定されていません")
        return self._command_body

    @command_body.setter
    def command_body(self, value: dict):
        if not isinstance(value, dict):
            raise ValueError("command_bodyは辞書型で指定してください")
        self._body_size = len(str(value).encode("utf-8"))
        self._command_body = value

    def get_body_to_str(self) -> str:
        """
        コマンドのボディを文字列に変換

        Returns:
            str: コマンドのボディ
        """
        self.convert_body()
        return json.dumps(self.command_body)

    @property
    def command_header(self) -> str:
        """
        コマンドのヘッダーを生成する

        Returns:
            str: コマンドのヘッダー
        """
        return f"{self.command_name} {self.body_size}"

    def convert_body(self) -> dict:
        """
        コマンドのボディを生成
        """
        raise NotImplementedError("convert_bodyメソッドが実装されていません")


if __name__ == "__main__":
    command = CommandBase()
    command.command_name = "CONTROL"
    # command.body_size = 100.0
    command.command_body = {
        "object_id": "1",
        "action": "start",
        "action_parameters": {
            "param1": "value1",
            "param2": "value2",
        },
    }
    print(command)
    # print(command.convert_body())
