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

function ableButton(button, enabled, className) {
    button.disabled = ! enabled;
    if (className != undefined) {
         button.className = className;
    }
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
        ableButton(checkButton, false, "opProgress");
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
        ableButton(uncompressButton, false, "opProgress");
        let url = urlStart + '?action=unpack&relative-path=' + selectedFile;
        loadJSON(url, function () {
            let preingestSessionId = this.response.sessionId;
            
            let timer = setInterval(function () {
                let pollURL = urlStart + '?action=check-for-file-with-ok&relative-path=' + preingestSessionId + "/UnpackTarHandler.json";
                fileOKCheckPoller(timer, pollURL, function() {
                     ableButton(uncompressButton, true, "opSuccess");
                     document.getElementById('proceedmessage').style.display = "block";
                }, function() {
                    ableButton(uncompressButton, true, "opFailure");
                });
            },
            pollIntervalMS);
        });
    }
}

function doOperationsButton(clickedButton, urlStart, preingestSessionId, actionString, requiredJSONFile, pollIntervalMS) {
    ableButton(clickedButton, false, "opProgress");
    
    let url = urlStart + "?action=" + actionString + "&sessionid=" + preingestSessionId;
    loadJSON(url, function () {
        let timer = setInterval(function () {
            let pollURL = urlStart + '?action=check-for-file&relative-path=' + preingestSessionId + "/" + requiredJSONFile;
            fileOKCheckPoller(timer, pollURL, function() {
                ableButton(clickedButton, true, "opSuccess");
            }, function() {
                    ableButton(clickedButton, true, "opFailure");
            });
        },
        pollIntervalMS);
    });
}

function fileOKCheckPoller(timer, url, successFunction, failFunction) {
    loadJSON(url, function () {
        let code = this.response.code;
        if (code != null) {
            clearInterval(timer);
            
            if (code !== "OK") {
                showError("De statuscode is niet OK maar " + code);
                if (failFunction != undefined) {
                    failFunction.call(this);
                }
            } else {
                if (successFunction != undefined) {
                    successFunction.call(this);
                }
            }
        }
    });
}

function fileChecksumPoller(timer, url, requiredChecksumType, requiredChecksumValue, checkButton, uncompressButton) {
    loadJSON(url, function () {
        if (this.response.sessionId != null) {
            clearInterval(timer);
            
            let checksumType = this.response.checksumType;
            let checksumValue = this.response.checksumValue;
            
            if ((checksumType !== requiredChecksumType) || (checksumValue !== requiredChecksumValue)) {
                ableButton(checkButton, true, "opFailure");
                showError("Het checksumtype en/of de checksumwaarde komen niet overeen; vereist/gevonden: " + requiredChecksumType + "/" + checksumType + ", " + requiredChecksumValue + "/" + checksumValue);
            } else {
                ableButton(checkButton, false, "opSuccess");
                ableButton(uncompressButton, true);
            }
            
        }
    });
}