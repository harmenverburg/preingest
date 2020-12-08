function getRadioValue(radioName) {
    let radios = document.getElementsByName(radioName);
    let value = null;
    for (var i = 0; i < radios.length; i++) {
        if (radios[i].checked) {
            value = radios[i].value;
            break;
        }
    }
    return value;
}

function loadJSON(url, onLoadFunction) {
    let request = new XMLHttpRequest();
    request.onload = onLoadFunction;
    request.open('GET', url);
    request.responseType = 'json';
    request.send();
}

function doCheckButton(urlStart, checksumTypeFieldId, selectedFileFieldId) {
    let checksumSelect = document.getElementById(checksumTypeFieldId);
    let checksumType = checksumSelect.value;
    let selectedFile = getRadioValue(selectedFileFieldId);
    
    console.log("urlStart=" + urlStart + ", checksumTypeFieldId=" + checksumTypeFieldId + ", checksumSelect=" + checksumSelect + ",checksumType=" + checksumType + ", selectedFile=" + selectedFile);
    if ((checksumType !== "") && (selectedFile !== null)) {
        let url = urlStart + checksumType + "/" + selectedFile;
        console.log("url=" + url);
        loadJSON(url, function () {
            console.log("response.sessionId=" + this.response.sessionId);
        });
    }
    
    let timer = setInterval(timerFunction, 1000);
    
    function timerFunction() {
        // roep een alternatief voor action aan, die monitort of een bepaalde file er is
        // wijzig de action zodanig dat alleen /action/calculate/... wordt aangeroepen
        // zodra de file er is, clearInterval(timer) aanroepen, of myStopFunction
        // https://www.w3schools.com/jsref/met_win_setinterval.asp
    }
    
    function myStopFunction() {
        clearInterval(timer);
    }
}