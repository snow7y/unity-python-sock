from unity_python_sock.commands.core import CommandBase

class PingCommand(CommandBase):
    """
    Pingコマンド
    """

    def __init__(self):
        super().__init__(
            command_name="PING",
        )

    def convert_body(self) -> dict:
        """
        コマンドのボディを生成
        """
        body = {}
        self.command_body = body
        return body
    
if __name__ == "__main__":
    command = PingCommand()
    print(command.get_command())