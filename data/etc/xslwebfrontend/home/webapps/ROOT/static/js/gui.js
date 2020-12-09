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

function ableRadios(radioName, enabled) {
    let radios = document.getElementsByName(radioName);
    let value = null;
    for (var i = 0; i < radios.length; i++) {
        radios[i].disabled = ! enabled;
    }
}

function showError(msg) {
    alert(msg);
}

function loadJSON(url, onLoadFunction) {
    let request = new XMLHttpRequest();
    request.onload = onLoadFunction;
    request.open('GET', url);
    request.responseType = 'json';
    request.send();
}

function doCheckButton(checkButton, uncompressButtonId, urlStart, checksumTypeFieldId, checksumValueFieldId, selectedFileFieldId, pollIntervalMS) {
    let fileSelect = document.getElementById(selectedFileFieldId);
    let checksumType = document.getElementById(checksumTypeFieldId).value;
    let checksumValue = document.getElementById(checksumValueFieldId).value.trim();
    let selectedFile = getRadioValue(selectedFileFieldId);
    let uncompressButton = document.getElementById(uncompressButtonId);
    
    let url = urlStart + '?action=calculate&checksum-type=' + checksumType + '&relative-path=' + selectedFile;
    
    if ((selectedFile != null) && (checksumType !== '') && checksumValue !== '') {
        checkButton.disabled = true;
        ableRadios(selectedFileFieldId, false);
        
        loadJSON(url, function () {
            let preingestSessionId = this.response.sessionId;
            
            let timer = setInterval(function () {
                let pollURL = urlStart + '?action=check-for-tar-json-file&relative-path=' + selectedFile + '&preingest-session-id=' + preingestSessionId;
                fileChecksumPoller(timer, pollURL, checksumType, checksumValue, checkButton, uncompressButton);
            },
            pollIntervalMS);
        });
    } else {
        showError("De volgende gegevens zijn vereist: bestandsnaam, checksumtype en checksumwaarde (aangeleverd door de zorgdrager)");
    }
}

function doUncompressButton(uncompressButton, urlStart, selectedFileFieldId, pollIntervalMS) {
    let selectedFile = getRadioValue(selectedFileFieldId);
    
    if (selectedFile != null) {
        // TODO eigenlijk ook checken of de checksum werkelijk is berekend, zodat we niet van de gui afhankelijk zijn...
        uncompressButton.disabled = true;
        let url = urlStart + '?action=unpack&relative-path=' + selectedFile;
        loadJSON(url, function () {
            let preingestSessionId = this.response.sessionId;
            
            let timer = setInterval(function () {
                let pollURL = urlStart + '?action=check-for-file-with-ok&relative-path=' + preingestSessionId + "/UnpackTarHandler.json";
                fileOKCheckPoller(timer, pollURL, function() {
                document.getElementById('proceedmessage').style.display = "block";
                });
            },
            pollIntervalMS);
        });
    }
}

function fileOKCheckPoller(timer, url, successFunction) {
    loadJSON(url, function () {
        if (this.response.sessionId != null) {
            console.log("json file has been loaded");
            clearInterval(timer);
            let code = this.response.code;
            if (code !== "OK") {
                showError("De statuscode is niet OK maar " + code);
            } else {
                if (successFunction != undefined) {
                    console.log("voor call, this=" + this);
                    successFunction.call(this);
                }
            }
        }
    });
}

function fileChecksumPoller(timer, url, requiredChecksumType, requiredChecksumValue, checkButton, uncompressButton) {
    loadJSON(url, function () {
        if (this.response.sessionId != null) {
            console.log("json file with checksum has been loaded");
            clearInterval(timer);
            
            let checksumType = this.response.checksumType;
            let checksumValue = this.response.checksumValue;
            
            if ((checksumType !== requiredChecksumType) || (checksumValue !== requiredChecksumValue)) {
                showError("Het checksumtype en/of de checksumwaarde komen niet overeen; vereist/gevonden: " + requiredChecksumType + "/" + checksumType + ", " + requiredChecksumValue + "/" + checksumValue);
            }
            
            uncompressButton.disabled = false;
        }
    });
}