from unity_python_sock.commands.core import CommandBase


class ControlCommand(CommandBase):
    """
    制御コマンドを管理するクラス
    """

    def __init__(self, object_id: str | None = None, action: str | None = None, action_parameters: dict | None = None):
        super().__init__(
            command_name="CONTROL",
        )
        self._object_id = object_id
        self._action = action
        self._action_parameters = action_parameters

    @property
    def object_id(self) -> str:
        if self._object_id is None:
            raise ValueError("object_idが設定されていません")
        return self._object_id

    @object_id.setter
    def object_id(self, value: str):
        if not isinstance(value, str):
            raise ValueError("object_idは文字列で指定してください")
        self._object_id = value

    @property
    def action(self) -> str:
        if self._action is None:
            raise ValueError("actionが設定されていません")
        return self._action

    @action.setter
    def action(self, value: str):
        if not isinstance(value, str):
            raise ValueError("actionは文字列で指定してください")
        self._action = value

    @property
    def action_parameters(self) -> dict:
        if self._action_parameters is None:
            raise ValueError("action_parametersが設定されていません")
        return self._action_parameters

    @action_parameters.setter
    def action_parameters(self, value: dict):
        if not isinstance(value, dict):
            raise ValueError("action_parametersは辞書型で指定してください")
        self._action_parameters = value

    def convert_body(self) -> dict:
        """
        コマンドのボディを生成
        """
        body = {
            "object_id": self.object_id,
            "action": self.action,
            "action_parameters": self.action_parameters,
        }
        self.command_body = body
        return body


if __name__ == "__main__":
    command = ControlCommand()
    command.object_id = "test"
    command.action = "move"
    command.action_parameters = {
        "speed": 100,
        "direction": "forward",
    }
    print(command)

    print("\n------------------------\n")

    command.convert_body()
    print(command)
