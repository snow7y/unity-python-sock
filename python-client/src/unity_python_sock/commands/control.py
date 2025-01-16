from unity_python_sock.commands.core import CommandBase


class ControlCommand(CommandBase):
    """
    制御コマンドを管理するクラス
    """

    def __init__(self):
        super().__init__(
            command_name="CONTROL",
        )
        self.object_id = None
        self.action = None
        self.action_parameters = {}

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
    command.object_id = 1
    command.action = "start"
    command.action_parameters = {
        "speed": 100,
        "direction": "forward",
    }
    print(command)

    print("\n------------------------\n")

    command.convert_body()
    print(command)
