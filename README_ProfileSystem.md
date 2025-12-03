# Program Manager - Profile System

## ?? **Profile-Based Architecture**
Your Program Manager now uses a **Profile system** for managing different sets of groups and shortcuts!

## ?? **New Menu Structure**

### **Profile Menu**
```
Profile
??? Main                    ? Default profile (Groups subfolder)
??? [Custom Profiles]       ? Added via "Add Profile..."
??? ?????????????????
??? Add Profile...          ? Browse to any folder
??? Delete Profile          ? Remove profile reference
```

### **Key Features**
? **Main Profile** - Points to `ApplicationFolder/Groups/`  
? **Custom Profiles** - Point to any folder location  
? **Root .lnk Handling** - Files in root become "Main" group  
? **Centralized Settings** - All .ini files in app folder  
? **Profile Memory** - Remembers last used profile  

## ?? **How It Works**

### **1. Main Profile (Default)**
- **Location:** `ProgramManagerVC.exe` folder ? `Groups` subfolder
- **Purpose:** Default location for groups and shortcuts
- **Cannot be deleted:** Always available as fallback

### **2. Custom Profiles**
- **Created via:** Profile ? Add Profile...
- **Dialog Title:** "NRG Speciality"
- **Function:** Browse to any folder containing groups
- **Storage:** Profile references saved in `Progman.ini`

### **3. Root .lnk File Handling**
When you select a folder with `.lnk` files directly in the root:
- **Creates:** Virtual "Main" group window
- **Contains:** All root-level shortcuts
- **Plus:** Any subfolders become additional groups

### **4. Settings Management**
- **Group Settings:** `GroupName.ini` in application folder
- **App Settings:** `Progman.ini` in application folder
- **Profile Data:** Stored in `[Profiles]` section

## ?? **File Structure Examples**

### **Main Profile Structure**
```
ProgramManagerVC.exe
??? Groups/                 ? Main profile location
?   ??? Office/
?   ?   ??? Word.lnk
?   ??? Games/
?       ??? Solitaire.lnk
??? Office.ini              ? Window settings
??? Games.ini               ? Window settings
??? Progman.ini             ? App & profile settings
```

### **Custom Profile Structure**
```
D:\MyApps/                  ? Custom profile location
??? test.lnk                ? Root files ? "Main" group
??? Graphics/               ? Subfolder ? "Graphics" group
?   ??? Photoshop.lnk
??? Audio/                  ? Subfolder ? "Audio" group
    ??? Audacity.lnk

ProgramManagerVC.exe folder:
??? Main.ini                ? Settings for root shortcuts
??? Graphics.ini            ? Settings for Graphics group
??? Audio.ini               ? Settings for Audio group
??? Progman.ini             ? Profile: "MyApps"="D:\MyApps\"
```

## ?? **Usage Scenarios**

### **Scenario 1: Multiple Project Folders**
```
Profile ? Add Profile... ? "Work Projects"
Select: C:\Work\ClientA\Tools\

Result: Each subfolder becomes a group
Settings: Stored locally in app folder
```

### **Scenario 2: Shared Network Location**
```
Profile ? Add Profile... ? "Shared Tools"
Select: \\Server\SharedApps\

Result: Network folders accessible as groups
Settings: Local to your machine
```

### **Scenario 3: Root Shortcuts**
```
Select folder with .lnk files in root:
D:\Utilities\
??? Calculator.lnk          ? Goes to "Main" group
??? Notepad.lnk            ? Goes to "Main" group
??? Advanced/              ? Becomes "Advanced" group
    ??? RegEdit.lnk

Result: "Main" + "Advanced" groups
```

## ?? **Technical Details**

### **Progman.ini Structure**
```ini
[Application]
current_profile=MyCustomProfile
window_width=800
window_height=600

[Profiles]
MyCustomProfile=D:\MyApps
WorkProfile=C:\Work\Tools
NetworkProfile=\\Server\Apps
```

### **Group Settings (GroupName.ini)**
```ini
[Window]
Status=1
X=100
Y=100
Width=400
Height=300

[Group]
Name=Office
```

### **Profile Benefits**
- **No file copying** - Profiles are just references
- **Multiple contexts** - Switch between different sets
- **Network support** - Can reference network locations
- **Portable settings** - App settings travel with executable

## ?? **Migration Path**

1. **Existing Groups folder** ? Automatically becomes "Main" profile
2. **Old Settings.ini** ? Migrated to `Progman.ini`
3. **Group.ini files** ? Moved to app folder as `GroupName.ini`

## ?? **Testing the System**

1. **Launch Program Manager**
2. **Profile ? Add Profile...**
3. **Select TestFolders directory**
4. **Name it:** "TestProfile"
5. **See:** Office, Games, Development, and Main (root .lnk files)
6. **Check:** Office.ini, Games.ini, etc. in app folder

---
*Your Program Manager now supports flexible profile-based group management! ??*