from unity_python_sock.commands.core import CommandBase

class PreviousCommand(CommandBase):
    """
    前のオブジェクトに変更するコマンド
    """

    def __init__(self):
        super().__init__(
            command_name="PREVIOUS",
        )

    def convert_body(self) -> dict:
        """
        コマンドのボディを生成
        """
        # body = {
        #     "hoge": "fuga"
        # }
        body = {}
        self.command_body = body
        return body
    

if __name__ == "__main__":
    command = PreviousCommand()
    print(command.get_command())
