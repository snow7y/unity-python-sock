class CommandBase:
    """
    コマンドの基底クラス
    """

    def __init__(self, command_name: str | None = None, command_body_size: int = 0, command_parameters: dict | None = None):
        self.command_name = command_name
        self.command_body_size = command_body_size
        # self.command_parameters: dict | None = None
        self.command_body = {}

    # パラメータをヘッダーの形に変更
    # def _convert_parameters_to_header(self) -> str:
    #     """
    #     パラメータをヘッダーの形に変換
    #     """
    #     parameters = ""
    #     for _, value in self.command_parameters.items():
    #         if value is not None:
    #             parameters += f"{value} "
    #     return parameters

    @staticmethod
    def convert_header(self) -> str:
        """
        コマンドのヘッダーを生成
        """
        return f"{self.command_name} {self.command_body_size}\n"

    def convert_body(self) -> str:
        """
        コマンドのボディを生成
        """
        raise NotImplementedError("convert_bodyメソッドが実装されていません")