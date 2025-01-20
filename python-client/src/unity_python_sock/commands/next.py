from unity_python_sock.commands.core import CommandBase

class NextCommand(CommandBase):
    """
    次のオブジェクトに変更するコマンド
    """

    def __init__(self):
        super().__init__(
            command_name="NEXT",
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
    command = NextCommand()
    print(command.get_command())
