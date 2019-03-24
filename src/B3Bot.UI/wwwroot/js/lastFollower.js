"use strict";

//var connection = new signalR.HubConnectionBuilder().withUrl("/b3BotHub").build();

//connection.on("NewChatMessage", function (chatMessage) {
//    var msg = document.createElement('div');

//    var id = +(new Date());
//    msg.id = 'msg' + id.toString();

//    msg.classList.add('chatMessage');
//    msg.innerHTML = chatMessage.hubMessage;

//    $('#msg' + id).hide();
//    $(".chatBox").append(msg);

//    calcPositions(msg);

//    $('#msg' + id).fadeIn('slow');

//    setTimeout(function (id) {
//        $('#msg' + id).fadeOut('slow', () => {
//            $('#msg' + id).remove();
//        });
//    }, 50000, id);
//});

//connection.start();
