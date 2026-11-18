from bashrun.bash import bash


def main() -> None:
    bash("uv run --locked preflight-python")


if __name__ == "__main__":
    main()
