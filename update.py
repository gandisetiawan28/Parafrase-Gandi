import re

def update_installer():
    with open('installer.cs', 'r', encoding='utf-8') as f:
        content = f.read()
    
    with open('new_ui.cs', 'r', encoding='utf-8') as f:
        new_ui = f.read()

    # Split the original content
    start_marker = "        // --- MD3 DESIGN TOKENS (Match HTML v7.0) ---"
    end_marker = "        // ======== CORE LOGIC ========"
    
    start_idx = content.find(start_marker)
    end_idx = content.find(end_marker)
    
    if start_idx == -1 or end_idx == -1:
        print("Markers not found!")
        return

    # Splice
    updated_content = content[:start_idx] + new_ui + "\n" + content[end_idx:]
    
    with open('installer.cs', 'w', encoding='utf-8') as f:
        f.write(updated_content)
        
    print("Successfully updated installer.cs!")

if __name__ == '__main__':
    update_installer()
