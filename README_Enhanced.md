# Enhanced File-Based Program Manager

## ?? **Latest Update: Load Folder Functionality**
Your Program Manager now supports loading groups from any folder location!

## ?? **New Architecture - Flexible Location**

### **Folder Structure (Any Location)**
```
[Selected Directory]/
??? Office/
?   ??? Word.lnk
?   ??? Excel.lnk
??? Games/
?   ??? Solitaire.lnk
??? Development/
?   ??? IDE.lnk
??? Utilities/
    ??? Tools.lnk

[Application Directory]/
??? ProgramManagerVC.exe
??? Settings.ini            ? App settings
??? Office.ini              ? Office group settings
??? Games.ini               ? Games group settings
??? Development.ini         ? Development group settings
??? Utilities.ini           ? Utilities group settings
```

## ?? **How The New System Works**

### **Load Folder... Feature**
1. **File ? Load Folder...** opens "NRG Speciality" dialog
2. **Select any directory** containing subfolders
3. **Subfolders become groups** (Office, Games, etc.)
4. **.lnk files** found recursively in each subfolder
5. **Group settings** stored as `.ini` files in application folder

### **Benefits of New Structure**
? **Flexible Location** - Load groups from anywhere  
? **Centralized Settings** - All .ini files in app folder  
? **Portable Groups** - Copy group folders anywhere  
? **Persistent Location** - Remembers last loaded folder  
? **Clean Separation** - Groups separate from settings  

## ?? **Usage Examples**

### **Loading Different Projects**
```
File ? Load Folder...
??? Select: D:\MyProjects\
    ??? WebDev/
    ??? GameDev/
    ??? Scripts/
```
**Result:** WebDev, GameDev, Scripts become groups

### **Using Network Locations**
```
File ? Load Folder...
??? Select: \\Server\SharedTools\
    ??? Databases/
    ??? Editors/
    ??? Utilities/
```
**Result:** Load shared tools from network

### **Organizing by Client/Project**
```
File ? Load Folder...
??? Select: C:\Work\ClientA\
    ??? Phase1/
    ??? Phase2/
    ??? Tools/
```

## ?? **Technical Details**

### **INI File Naming**
- **Group folder:** `Office` ? **INI file:** `Office.ini`
- **Stored in:** Application directory (always)
- **Contains:** Window position, size, state

### **Settings Persistence**
- **Last loaded folder** remembered between sessions
- **Window positions** saved automatically
- **Group configurations** portable with application

### **Migration from Old System**
- **Existing Groups/** folder still works
- **Old Group.ini files** automatically converted
- **Seamless transition** to new system

## ?? **Example Workflow**

1. **Organize shortcuts** in any folder structure:
   ```
   D:\MyApps\
   ??? Graphics/
   ?   ??? Photoshop.lnk
   ?   ??? GIMP.lnk
   ??? Audio/
       ??? Audacity.lnk
       ??? VLC.lnk
   ```

2. **Load in Program Manager:**
   - File ? Load Folder...
   - Select `D:\MyApps\`
   - Graphics and Audio groups appear

3. **Settings stored locally:**
   ```
   ProgramManagerVC.exe directory:
   ??? Graphics.ini     ? Window settings
   ??? Audio.ini        ? Window settings
   ??? Settings.ini     ? App remembers D:\MyApps\
   ```

## ?? **Dialog Title**
The folder selection dialog displays **"NRG Speciality"** as requested.

## ?? **Backwards Compatibility**
- **Existing setups** continue to work
- **Groups folder** in app directory still supported
- **Automatic migration** of settings format

---
*Your enhanced Program Manager now supports flexible folder loading with centralized settings! ??*