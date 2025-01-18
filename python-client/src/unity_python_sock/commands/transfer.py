import os

from unity_python_sock.commands.core import CommandBase


class TransferCommand(CommandBase):
    """
    ファイル転送コマンドを管理するクラス
    """

    def __init__(self, file_path: str | None = None):
        super().__init__(
            command_name="TRANSFER",
        )
        self.file_path = file_path

    @property
    def file_path(self) -> str:
        if self._file_path is None:
            raise ValueError("file_pathが設定されていません")
        return self._file_path

    @file_path.setter
    def file_path(self, value: str):
        if not isinstance(value, str):
            raise ValueError("file_pathは文字列で指定してください")
        if not os.path.exists(value):
            raise FileNotFoundError(f"ファイルが見つかりません: {value}")
        self._file_path = value

    @property
    def file_name(self) -> str:
        return os.path.basename(self.file_path)

    @property
    def file_size(self) -> int:
        return os.path.getsize(self.file_path)

    def convert_body(self) -> dict:
        """
        コマンドのボディを生成
        """
        body = {
            "file_name": self.file_name,
            "file_size": self.file_size,
        }
        self.command_body = body
        return body


if __name__ == "__main__":
    command = TransferCommand("README.md")
    print(command)

    print("\n------------------------\n")

    command.convert_body()
    print(command)
