# About the SIP Creator and why it may seem to be missing

The SIP_Creator folder is **not** included in this Git repository.

You can download it from the Preservica website, the user group or the web interface of the SAAS environment. Be sure to download
version 5.11, as 5.10 does not use the information in XIP metadata sidecar files.

After downloading and extracting it, rename the folder "SIP Creator" into "SIP_Creator" (i.e., no space in the name).

**IMPORTANT:**

The shell script *createsip* is **not** used. A modified copy (**nha-createsip**) is part of the XSLWeb sipcreator webapp. This
copy is used instead.

In case of new versions of the SIP Creator, compare nha-createsip with the new Preservica version and adapt if needed.
