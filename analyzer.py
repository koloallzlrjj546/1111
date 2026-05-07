import json
import sys


def write_output(path, data):
    with open(path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)


def main():
    if len(sys.argv) != 3:
        print("Usage: python analyzer.py input.json output.json")
        return 1

    input_path = sys.argv[1]
    output_path = sys.argv[2]

    try:
        with open(input_path, "r", encoding="utf-8") as f:
            data = json.load(f)
    except Exception as e:
        write_output(output_path, {"ok": False, "error": "Не удалось прочитать input.json: " + str(e)})
        return 1

    if "numbers" not in data:
        write_output(output_path, {"ok": False, "error": "В input.json нет поля 'numbers'."})
        return 1

    numbers = data["numbers"]
    if not isinstance(numbers, list):
        write_output(output_path, {"ok": False, "error": "'numbers' должен быть списком."})
        return 1

    if len(numbers) == 0:
        write_output(output_path, {"ok": False, "error": "Список чисел пустой."})
        return 1

    try:
        nums = [float(x) for x in numbers]
    except Exception:
        write_output(output_path, {"ok": False, "error": "В списке есть нечисловые значения."})
        return 1

    s = sum(nums)
    mn = min(nums)
    mx = max(nums)
    avg = s / len(nums)

    result = {
        "ok": True,
        "stats": {
            "count": len(nums),
            "sum": s,
            "avg": avg,
            "min": mn,
            "max": mx
        }
    }

    try:
        write_output(output_path, result)
    except Exception as e:
        print("Не удалось записать output.json:", e)
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())

