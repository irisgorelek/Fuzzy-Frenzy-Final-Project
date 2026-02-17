import os

# === SETTINGS ===
OUTPUT_FILE = "cs_dump.txt"
MAX_FILE_SIZE_KB = 200  # Skip very large files
EXCLUDED_FOLDERS = {"Library", "Temp", "Logs", "obj", ".git", "Packages","Tests"}

def should_exclude(path):
    parts = set(path.split(os.sep))
    return any(folder in parts for folder in EXCLUDED_FOLDERS)

def main():
    project_root = os.getcwd()
    output_path = os.path.join(project_root, OUTPUT_FILE)

    with open(output_path, "w", encoding="utf-8") as outfile:
        for root, dirs, files in os.walk(project_root):
            # Remove excluded directories from traversal
            dirs[:] = [d for d in dirs if d not in EXCLUDED_FOLDERS]

            for file in files:
                if file.endswith(".cs"):
                    full_path = os.path.join(root, file)

                    if should_exclude(full_path):
                        continue

                    size_kb = os.path.getsize(full_path) / 1024
                    if size_kb > MAX_FILE_SIZE_KB:
                        outfile.write(f"\n\n===== SKIPPED (too large) =====\n{full_path}\n")
                        continue

                    outfile.write("\n\n" + "="*80 + "\n")
                    outfile.write(f"FILE: {full_path}\n")
                    outfile.write("="*80 + "\n\n")

                    try:
                        with open(full_path, "r", encoding="utf-8") as infile:
                            outfile.write(infile.read())
                    except Exception as e:
                        outfile.write(f"ERROR READING FILE: {e}\n")

    print(f"\nDone. Output written to: {output_path}")

if __name__ == "__main__":
    main()
