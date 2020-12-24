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

function doCheckButton(checkButton, uncompressButtonId, checksumTypeFieldId, checksumValueFieldId, selectedFileFieldId, pollIntervalMS) {
    let mainDiv = document.getElementById("main-div");
    let actionsPrefix = mainDiv.dataset.prefix;

    let fileSelect = document.getElementById(selectedFileFieldId);
    let requiredChecksumType = document.getElementById(checksumTypeFieldId).value;
    let requiredChecksumValue = document.getElementById(checksumValueFieldId).value.trim();
    let selectedFile = getRadioValue(selectedFileFieldId);
    let uncompressButton = document.getElementById(uncompressButtonId);
    
    let url = actionsPrefix + '?action=calculate&checksum-type=' + requiredChecksumType + '&relative-path=' + encodeURI(selectedFile);
    
    if ((selectedFile != null) && (requiredChecksumType !== '') && requiredChecksumValue !== '') {
        ableButton(checkButton, false, "opProgress");
        ableRadios(selectedFileFieldId, false);
        
        loadJSON(url, function () {
            let actionId = this.response.actionId;
            
            let timer = setInterval(function () {
                let pollURL = actionsPrefix + "?action=checksum-action-result&action-id=" + actionId + "&relative-path=" + encodeURI(selectedFile);
                
                actionResultPoller(timer, pollURL, "completed", function() {
                     let checksumType = this.response.checksumType;
                     let checksumValue = this.response.checksumValue;
                
                     if ((checksumType !== requiredChecksumType) || (checksumValue !== requiredChecksumValue)) {
                         ableButton(checkButton, true, "opFailure");
                         showError("Het checksumtype en/of de checksumwaarde komen niet overeen; vereist/gevonden: " + requiredChecksumType + "/" + checksumType + ", " + requiredChecksumValue + "/" + checksumValue);
                     } else {
                         ableButton(checkButton, false, "opSuccess");
                         ableButton(uncompressButton, true);
                     }
                }, function() {
                    ableButton(checkButton, true, "opFailure")
                    ableButton(uncompressButton, false);
                });
            },
            pollIntervalMS);
        });
    } else {
        showError("De volgende gegevens zijn vereist: bestandsnaam, checksumtype en checksumwaarde (aangeleverd door de zorgdrager)");
    }
}

function doUncompressButton(uncompressButton, selectedFileFieldId, pollIntervalMS) {
    let mainDiv = document.getElementById("main-div");
    let actionsPrefix = mainDiv.dataset.prefix;

    let selectedFile = getRadioValue(selectedFileFieldId);
    
    if (selectedFile != null) {
        // TODO eigenlijk ook checken of de checksum werkelijk is berekend, zodat we niet van de gui afhankelijk zijn...
        ableButton(uncompressButton, false, "opProgress");
        let url = actionsPrefix + '?action=unpack&relative-path=' + encodeURI(selectedFile);
        loadJSON(url, function () {
            let preingestSessionId = this.response.sessionId;
            let actionId = this.response.actionId;
            let requiredCode = "Unpack";
            let jsonResultFile = "UnpackTarHandler.json";
            
            let timer = setInterval(function () {
                let pollURL = actionsPrefix + "?action=action-result&action-id=" + actionId + "&session-id=" + preingestSessionId + "&jsonresultfile=" + encodeURI(jsonResultFile);
                
                actionResultPoller(timer, pollURL, "completed", function() {
                     let code = this.response.code;
                
                     if (code !== requiredCode) {
                         ableButton(uncompressButton, true, "opFailure");
                         showError("De geretourneerde code is niet " + requiredCode + " maar " + code);
                     } else {
                         ableButton(uncompressButton, false, "opSuccess");
                         let a = document.getElementById('proceedlink');
                         let href = a.href;
                         a.href = href.replace("operations/", "operations/" + preingestSessionId);
                         document.getElementById('proceedmessage').style.display = "block";
                     }
                }, function() {
                    ableButton(uncompressButton, true, "opFailure");
                });
            },
            pollIntervalMS);
        });
    }
}

function doOperationsButton(clickedButton, pollIntervalMS) {
    let mainDiv = document.getElementById("main-div");
    let guid = mainDiv.dataset.guid;
    let actionsPrefix = mainDiv.dataset.prefix;
    
    ableButton(clickedButton, false, "opProgress");
    
    let url = actionsPrefix + "?action=" + clickedButton.dataset.action + "&sessionid=" + guid;
    console.log("doOperationsButton, url=" + url);
    loadJSON(url, function () {
        let preingestSessionId = this.response.sessionId;
        let actionId = this.response.actionId;
        let requiredCode = "Unpack";
        let jsonResultFile = clickedButton.dataset.waitforfile;
        
        let timer = setInterval(function () {
            let pollURL = actionsPrefix + "?action=action-result&action-id=" + actionId + "&session-id=" + preingestSessionId + "&jsonresultfile=" + encodeURI(jsonResultFile);
            //console.log("doOperationsButton, pollURL=" + pollURL);
            actionResultPoller(timer, pollURL, "completed", function() {
                ableButton(clickedButton, true, "opSuccess");
            }, function() {
                ableButton(clickedButton, true, "opFailure");
            });
        },
        pollIntervalMS);
    });
}

function actionResultPoller(timer, url, requiredStatus, successFunction, failFunction) {
    loadJSON(url, function () {
        if (this.response != undefined && this.response.status != null) {
            clearInterval(timer);
            
            let status = this.response.status;
            
            if (status != requiredStatus) {
                showError("De statuscode is niet " + requiredStatus + " maar " + status);
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

