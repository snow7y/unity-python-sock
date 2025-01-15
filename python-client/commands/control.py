from commands.control import CommandBase
    

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

    def convert_body(self) -> str:
        """
        コマンドのボディを生成
        """
        return f"{self.object_id} {self.action} {self.action_parameters}\n"
