

import sys
import os
sys.path.append(os.path.join(os.path.dirname(__file__), "lib"))


import json

import Live





from _Framework.SubjectSlot import subject_slot

from _Framework.ControlSurface import ControlSurface
from _Framework.InputControlElement import *
from _Framework.ButtonElement import ButtonElement, ON_VALUE, OFF_VALUE
from _Framework.TransportComponent import TransportComponent

import MidiRemoteScript

from .Logger import Log





'''
class WsThread(threading.Thread):
    def __init__(self):
        super(WsThread, self).__init__()
        #global ws
        # Can setup other things before the thread starts
        self.ws = websocket.WebSocketApp("ws://localhost:9000")
        self.ws.on_open = self.on_open
        self.ws.on_message = self.on_message
        self.ws.on_error = self.on_error
        self.ws.on_close = self.on_close

    def run(self):
        self.ws.run_forever(sslopt={"cert_reqs": ssl.CERT_NONE , "check_hostname": False})


    def on_message(self, message):
        
        #self.ws.send("scroll done")
        parseMessage = json.loads(message)
        command = parseMessage['command']


        #Log.log("Execute %s" % self.data)
        #Log.log("Debug %s" % socket)

        

        if command == "scroll":
            direction = parseMessage['direction']
            Live.Application.get_application().view.scroll_view(int(direction),'Browser',False)            

            #attrs = vars(Live.Application.get_application())
            #result = "{'result':'" + ''.join("%s: %s /------/ " % item for item in list(attrs.items())) + "'}" 

            #self.sendMessage("scroll done")
            #self.ws.send("scroll done")
                             
     #   else:
      #      Log.log("Unknown command!")

    
    def on_open(self):
        Log.log("on_open called...")
        connectMessage = {"messag":"Ableton connected"}
        regMsg =  json.dumps(connectMessage)
        #self.ws.send("hello this is Ableton")

    def on_error(self, error):
        Log.log("error %s" % error)

    def on_close(self):
        Log.log("WS closed")
'''

class Logicraft(ControlSurface):
    def __init__(self, c_instance):
        #ControlSurface.__init__(self, c_instance)
        super(Logicraft, self).__init__(c_instance)
        with self.component_guard():
            self.__c_instance = c_instance
            Log.set_logger(self.log_message)
            Log.log('Logicraft starting up')
            
            

            self._set_suppress_rebuild_requests(True)
            self._suppress_send_midi = True

            self.set_up_controls()
            self.request_rebuild_midi_map()
            
            self._set_suppress_rebuild_requests(False) 
            self._active = True
            self._suppress_send_midi = False
            os.system('"C/Windows/System32/notepad.exe"')
            
 




    def set_up_controls(self):
        is_momentary = True
        self.crown = ButtonElement(is_momentary, MIDI_CC_TYPE, 0, 0)
        self._do_crown.subject = self.crown
        Log.log("crown mapping setup")
        

    @subject_slot('value')
    def _do_crown(self, value):
        assert value in range(128)
        #Log.log(value)
        direction = 0
        offset = value - 64
        if offset > 0:
            direction = 1
        #Log.log(offset)
        if abs(offset) > 0:
            for x in range(abs(offset)):
                Live.Application.get_application().view.scroll_view(direction,'Browser',False)
 
   
        
            
    #def receive_midi(self, midi_bytes):
        #debug_out("receive_midi() called: " + str(midi_bytes[0] & 240))
        #debug_out("receive_midi() note called: " + str(midi_bytes[1] ))
        #if midi_bytes[0] & 240 == MIDI_NOTE_ON_STATUS or midi_bytes[0] & 240 == MIDI_NOTE_OFF_STATUS:
        #    note = midi_bytes[1]
        #Log.log("Midi received yeahhh")
        #Log.log(midi_bytes[2])
            
                    
        #super(Logicraft, self).receive_midi(midi_bytes)



    def _focus_changed(self):
        global focusedView
        focusedView = Live.Application.get_application().get_document()
        result = "{'result':'" + str(focusedView) + "'}" 
        #Log.log(result)
        attrs = vars(focusedView)
        result = "{'result':'" + ''.join("%s: %s ------ " % item for item in list(attrs.items())) + "'}" 
        


    def disconnect(self):
        Log.log('Logicraft shutting down')
        ControlSurface.disconnect(self)


    def get_song(self):
        return Live.Application.get_application().get_document()

    def get_liveApplication(self):
        return Live.Application.get_application()
