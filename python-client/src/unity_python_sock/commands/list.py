from unity_python_sock.commands.core import CommandBase

class ListCommand(CommandBase):
    """
    オブジェクトリスト取得コマンド
    """

    def __init__(self):
        super().__init__(
            command_name="LIST",
        )

    def convert_body(self) -> dict:
        """
        コマンドのボディを生成
        """
        body = {}
        self.command_body = body
        return body
    

if __name__ == "__main__":
    command = ListCommand()
    print(command.get_command())
