# YtDlpWrapper

A desktop application for Windows that allows users to download video and audio through **yt-dlp** using a convenient graphical interface.  
The application runs locally on the user's computer, includes the required binaries, and is distributed via **MSIX**.

---

## Features

- Download video and audio from a link using `yt-dlp`
- Support for video formats: `mp4`, `webm`, `mkv`
- Support for audio formats: `mp3`, `m4a`, `opus`
- Video quality selection from `480p` up to `2160p`, including `Best` mode
- Automatic playlist detection and playlist downloading from a link
- Select the download folder using the system file picker
- Bundled `yt-dlp` and `ffmpeg` binaries distributed with the application
- Ability to update `yt-dlp` from within the application

---

## Release Contents

Project releases are planned to include:

- an application installer package in `MSIX` format
- an application signing certificate in `.cer` format

The application is signed with a custom certificate, so before installing the `MSIX` package for the first time, the certificate must be added to the trusted certificates in Windows.

---

## Certificate Installation

Before installing the application:

1. Download the `*.cer` certificate file from the project release.
2. Open the certificate file by double-clicking it.
3. Click `Install Certificate`.
4. Select `Local Machine` if installation is intended for all users, or `Current User` if installation is only for your account.
5. Choose `Place all certificates in the following store`.
6. Click `Browse` and select the `Trusted People` certificate store.
7. Complete the installation wizard and confirm the Windows warning if it appears.

After that, Windows will trust the signature of the `MSIX` package.

---

## Application Installation

After installing the certificate:

1. Download the appropriate `MSIX` package from the project release.
2. Open the `*.msix` file.
3. In the Windows installer window, click `Install`.
4. Wait for the installation to finish and launch the application from the Start menu.

If Windows reports that the publisher is not trusted, it means the certificate was not installed or was installed into the wrong certificate store.

---

## First Launch

After launching the application, you can:

- paste a link to a video or playlist
- choose the download mode: video or audio
- select the required format
- choose the video quality
- change the download folder in the settings if needed

---

## Technologies

- .NET 8
- WinUI 3
- Windows App SDK
- MSIX Packaging
- `yt-dlp`
- `ffmpeg`

---

## Third-Party Components

This application bundles third-party binaries, including `yt-dlp` and `ffmpeg`.  
These components are subject to their own licensing terms.  
For details, refer to the official upstream projects and license documents:

- https://github.com/yt-dlp/yt-dlp
- https://ffmpeg.org/legal.html
