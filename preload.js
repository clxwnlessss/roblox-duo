const {contextBridge,ipcRenderer}=require('electron');
contextBridge.exposeInMainWorld('duo',{call:(action,value)=>ipcRenderer.invoke('duo',action,value)});
