import sys
import os

import json
import Live
from threading import Thread 
import MidiRemoteScript

from _Framework.SubjectSlot import subject_slot
from _Framework.ControlSurface import ControlSurface
from _Framework.InputControlElement import *
from _Framework.ButtonElement import ButtonElement, ON_VALUE, OFF_VALUE
from _Framework.TransportComponent import TransportComponent

from .Logger import Log





class RunLogicraftAgent: 
    def __init__(self): 
        self._running = True
	
    def terminate(self): 
        self._running = False
    def run(self):
        os.system('"C:/ProgramData/Ableton/Live 11 Beta/Resources/MIDI Remote Scripts/Logicraft/LogicraftAgent/LogicraftAbleton.exe"')


class Logicraft(ControlSurface):
    def __init__(self, c_instance):
        ControlSurface.__init__(self, c_instance)
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

            c = RunLogicraftAgent() 
            t = Thread(target = c.run) 
            t.start() 
            c.terminate()
            
 




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
