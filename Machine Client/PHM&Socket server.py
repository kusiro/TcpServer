import pandas as pd
import numpy as np
import matplotlib.pylab as plt
from pandas import datetime 
from sklearn.feature_selection import SelectKBest,mutual_info_regression
import socket
import sys
import time
import _thread
import threading
import select
import os
from threading import Thread

global client_conns
global client_addrs
global i 
i = 0
client_conns = []*10
client_addrs = []*10
CONNECTION_LIST = []

def SOCKETNEW():

 global client_conns
 global client_addrs
 global i 
 i = 0
 client_conns = []*10
 client_addrs = []*10
 CONNECTION_LIST = []

 HOST = '192.168.0.19'  # Symbolic name meaning all available interfaces
 PORT = 9999 # Arbitrary non-privileged port
 
 s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
 print ('Socket created')

 #1為可重複使用已離線之addr
 s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

 #Bind socket to local host and port
 try:
   s.bind((HOST, PORT))
 except socket.error as msg:
   print ('Bind failed.')
   sys.exit()
 print ('Socket bind complete')

 s.setblocking(False) #設定非阻塞式
 #Start listening on socket
 s.listen(10)
 print ('Socket now listening')

 CONNECTION_LIST.append(s)

 #Function to broadcast chat messages to all connected clients
 def broadcast_data (sock, message):
     #Do not send the message to master socket and the client who has send us the message
     for socket in CONNECTION_LIST:
         if socket != s and socket != sock :
             try :
                 socket.send(message)
             except :
                 # broken socket connection may be, chat client pressed ctrl+c for example
                 socket.close()
                 CONNECTION_LIST.remove(socket)

 def clientthread(conn):
      
  #Sending message to connected client
  message = ('Welcome to the server. \n')
  conn.send(message.encode()) #send only takes string

 while 1:
      
  # Get the list sockets which are ready to be read through select
  read_sockets,write_sockets,error_sockets = select.select(CONNECTION_LIST,[],[])    
  
  #非阻塞式若直接使用s.accept會造成錯誤    
  try:    
    #wait to accept a connection - blocking call
    conn, addr = s.accept()
    # Add socket to the list of readable connections
    CONNECTION_LIST.append(conn)
    print ('Connected with ' + addr[0] + ':' + str(addr[1]))

    i += 1
    f = open('Machine 00'+str(i)+".csv",'wb') # Open in binary

    while (True):
      time.sleep(5)
      
      l = conn.recv(409600)

      while (l):
        f.write(l)
        l = conn.recv(409600)
    f.close()
    conn.close()
  
    #start new thread takes 1st argument as a function name to be run, second is the tuple of arguments to the function.
    _thread.start_new_thread(clientthread ,(conn, ))
  except:
    continue

  s.close()
 
def parser(x):
    return datetime.strptime('x', '%Y-%m-%d-%l-%s')

def PHM():
 df=pd.read_csv('Machine 00'+str(i)+'.csv',index_col=0,parse_dates=[0],engine='python')
 dfAA= abs(df)

 X = dfAA.iloc[:,0:7]
 X1=np.cumsum(X)
 X2=abs(X1)
 
 from sklearn.preprocessing import MinMaxScaler
 import pickle

 with open('weights/scaler.pickle','rb')as f:
     scaler=pickle.load(f)
    
 X_scaled=scaler.transform(X2)

 import pickle

 with open('weights/Xscaler.pickle','rb')as f:
    Xscaler=pickle.load(f)
 X2_scaled=Xscaler.transform(X2)

 from sklearn.neural_network import MLPRegressor
 import pickle

 with open('weights/mlp.pickle','rb') as f:
    mlp=pickle.load(f)
    
 y_pre = mlp.predict(X2_scaled)
 EHI=pd.DataFrame(y_pre)
 EHI.to_csv('Result 00'+str(i)+'.csv')
 
 fEHI = EHI.tail(1)
 fEHI = fEHI.iloc[0,0]
 fEHI = round(fEHI,2)
 fEHI = int(fEHI*100)
 print(fEHI)

 plt.figure(figsize=(15,6))
 plt.title('Equipment Status Index')
 plt.ylabel('Equipment Status Index')
 plt.xlabel('count')
 plt.grid(True)
 plt.plot(y_pre)
 plt.savefig(str(fEHI)+'.png')
 
 x=pd.DataFrame(df.iloc[:,1:6])
 print(x.shape)
 y_pre=pd.DataFrame(EHI)
 print(y_pre.shape)
 test=SelectKBest(score_func=mutual_info_regression, k=4)
 fit=test.fit(x,y_pre)
 np.set_printoptions(precision=1)

 Features=fit.transform(x)

 names=x.columns.values[test.get_support()]
 Scores=test.scores_[test.get_support()]
 names_scores=list(zip(names, Scores))
 ns_df=pd.DataFrame(data=names_scores,columns=['Feat_names','F_scores'])
 ns_df_scorted=ns_df.sort_values(['F_scores','Feat_names'],ascending=[False,True])
 
 print("F_scores=", fit.scores_)

 plt.rcParams.update(({'figure.autolayout':True}))

 fig,ax=plt.subplots(figsize=(10,6))
 ax.barh(names,Scores)
 labels=ax.get_xticklabels()
 #plt.step(labels, rotation=45,horizontalalignment='right')
 ax.set(xlim=[0,1], xlabel='Key Feature F_Score sorting', ylabel='Key Feature', title='Abattment Real key Feature Sorting')
 #print(Features[0:696,:])
 plt.savefig('Key.png')

 print("PHM done")

if __name__ == "__main__":

  t1 = threading.Thread(target=SOCKETNEW)
  t2 = threading.Thread(target=PHM)

  t1.start()

  while 1:
    try:
      while os.path.isfile('Machine 00'+str(i)+'.csv'): 
       time.sleep(6)
       t2.start()
    except:
      continue
   
