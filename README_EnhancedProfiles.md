# Program Manager - Enhanced Default Profiles & Behavior

## ?? **Latest Updates**
Your Program Manager now includes enhanced default profiles and improved group behavior!

## ?? **Default Profiles Available**

### **1. Main Profile**
- **Location:** `Application\Groups\` folder
- **Purpose:** Local program shortcuts and custom groups
- **Use Case:** Personal application organization

### **2. Start Menu (All Users)**
- **Location:** `C:\ProgramData\Microsoft\Windows\Start Menu\Programs`
- **Purpose:** System-wide Windows Start Menu shortcuts
- **Use Case:** Access all installed programs for all users

### **3. Start Menu (Current User)**
- **Location:** `%APPDATA%\Microsoft\Windows\Start Menu\Programs`
- **Purpose:** Current user's Start Menu shortcuts
- **Use Case:** Personal Start Menu items and user-installed apps

## ?? **Updated Behavior**

### **Root .lnk File Handling**
- **Before:** Root shortcuts ? "Main" group
- **After:** Root shortcuts ? "Programs" group
- **Benefit:** Better semantic naming, clearer purpose

### **New Group State**
- **Before:** New groups opened as windows
- **After:** New groups start MINIMIZED (as icons)
- **Benefit:** Cleaner desktop, less overwhelming when loading profiles

## ?? **Profile Usage Examples**

### **Accessing Windows Start Menu**
```
Profile ? Start Menu (Current User)
??? Programs group (root shortcuts)
??? Accessories folder ? Accessories group
??? System Tools folder ? System Tools group
??? Startup folder ? Startup group
```

### **Managing System Programs**
```
Profile ? Start Menu (All Users)  
??? Programs group (system shortcuts)
??? Microsoft Office folder ? Microsoft Office group
??? Adobe folder ? Adobe group
??? Games folder ? Games group
```

### **Personal Organization**
```
Profile ? Main
??? Programs group (custom shortcuts)
??? Work folder ? Work group
??? Personal folder ? Personal group
??? Utilities folder ? Utilities group
```

## ?? **Profile Switching Workflow**

1. **Launch Program Manager** - Starts with Main profile
2. **Profile Menu** - Shows all 3 default profiles
3. **Switch to Start Menu Profile** - Click to load Windows shortcuts
4. **Groups Load as Icons** - All groups minimized initially
5. **Double-click Icons** - Open specific groups as needed

## ??? **File Structure Examples**

### **Main Profile Structure**
```
Application\
??? Main.ini              ? Main profile settings
??? Groups\               ? Main profile folder
?   ??? shortcuts.lnk     ? Programs group
?   ??? Work\             ? Work group (minimized)
?   ??? Personal\         ? Personal group (minimized)
??? Progman.ini          ? Profile references
```

### **Start Menu Profile Structure**
```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\
??? shortcuts.lnk         ? Programs group
??? Accessories\          ? Accessories group (minimized)
??? System Tools\         ? System Tools group (minimized)
??? Startup\              ? Startup group (minimized)

Application\
??? Start Menu (Current User).ini  ? Profile settings
??? Progman.ini                    ? Profile references
```

## ?? **User Experience Improvements**

### **Cleaner Startup**
- **Default profiles ready** - No setup required
- **Groups minimized** - Desktop not cluttered
- **Quick access** - Double-click icons to open

### **Windows Integration**
- **Native Start Menu access** - Direct shortcut management
- **System-wide coverage** - All Users and Current User
- **Familiar locations** - Standard Windows paths

### **Flexible Organization**
- **Multiple contexts** - Personal, System, Work profiles
- **Easy switching** - One-click profile changes
- **Consistent behavior** - All new groups minimized

## ?? **Default Profile INI Structure**

### **Main.ini Example**
```ini
[Profile]
Name=Main
Path=C:\Users\User\AppData\Local\ProgramManagerVC\Groups

[Group_Programs]
Name=Programs
Status=0
X=100
Y=100
Width=400
Height=300
```

### **Start Menu Profile Example**
```ini
[Profile]
Name=Start Menu (Current User)
Path=C:\Users\User\AppData\Roaming\Microsoft\Windows\Start Menu\Programs

[Group_Programs]
Name=Programs
Status=0
X=100
Y=100

[Group_Accessories]
Name=Accessories
Status=0
X=120
Y=120
```

## ?? **Key Benefits**

### **Immediate Access**
? **Built-in Windows shortcuts** - No manual setup  
? **System integration** - Direct Start Menu access  
? **Multiple user contexts** - All Users and Current User  

### **Better Organization**
? **Semantic naming** - "Programs" vs "Main"  
? **Minimized groups** - Cleaner initial state  
? **Icon-based management** - Desktop remains uncluttered  

### **Enhanced Workflow**
? **Quick profile switching** - Instant context changes  
? **On-demand opening** - Open only needed groups  
? **Familiar paths** - Standard Windows locations  

---
*Your Program Manager now provides instant access to Windows Start Menu shortcuts with improved organization and cleaner behavior! ??*